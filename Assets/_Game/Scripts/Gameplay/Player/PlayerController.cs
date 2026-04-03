using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Interactor _interactor;
    [SerializeField] private InputReaderSO _inputReader;
    [SerializeField] private Transform _headBone;
    [SerializeField] private TransformEventChannelSO _headBoneTransformChannel;
    [SerializeField] private TransformEventChannelSO _targetTransformChannel;

    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private float _initJumpVelocity = 20f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _onAirSpeed = 10f;
    [SerializeField] private BoolEventChannelSO _togglePlayerInputEvent;
    public CharacterController CharacterController;
    public Vector3 MoveDirection { get; private set; }
    public Vector3 Movement;
    public Vector3 PreviousMovement;
    private IState _currentState;
    private IdleState _idleState;
    private MoveState _moveState;
    private JumpState _jumpState;
    private AttackState _attackState;
    private SitState _sitState;
    private EmoteState _emoteState;

    private bool _canJump = true;

    public bool CanInteract { get => _interactor.enabled; set => _interactor.enabled = value; }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            _interactor.gameObject.SetActive(false);
            return;
        }
        base.OnNetworkSpawn();
        Animator animator = GetComponent<Animator>();
        gameObject.GetOrAdd<ObjectFader>();
        _idleState = new IdleState(this, animator);
        _moveState = new MoveState(this, animator, _moveSpeed);
        _jumpState = new JumpState(this, animator, _onAirSpeed, _initJumpVelocity);
        _attackState = new AttackState(animator);
        _sitState = new SitState(this, animator);
        _emoteState = new EmoteState(this, animator);
        _currentState = _idleState;

        _inputReader.Move += HandleMove;
        _inputReader.Attack += HandleAttack;
        _inputReader.Interact += HandleInteract;
        _inputReader.Interact += _interactor.Interact;
        GameplayManager.Instance.OnStartGame += OnStartGame;
        GameplayManager.Instance.OnEndGame += OnEndGame;
        Emoter.OnEmoteSelected += HandleEmote;
        _targetTransformChannel.RaiseEvent(transform);
        _headBoneTransformChannel.RaiseEvent(_headBone);
        CameraController.SwitchCamMode(CameraMode.ThirdPerson);
        _togglePlayerInputEvent.OnEventRaised += HandleToggleInput;

        Respawn();
    }

    public void Respawn()
    {
        CharacterController.enabled = false;
        ToState(_idleState);
        GameObject respawnPoint = GameObject.FindGameObjectWithTag(Constant.RESPAWN_TAG);
        if (respawnPoint == null)
        {
            Debug.LogWarning("Respawn point not found!");
            transform.position = Vector3.zero;
        }
        else
        {
            Vector3 random = respawnPoint.transform.position + UnityEngine.Random.insideUnitSphere * 2f;
            transform.position = random;
        }
        CharacterController.enabled = true;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;
        base.OnNetworkDespawn();
        _inputReader.Move -= HandleMove;
        _inputReader.Attack -= HandleAttack;
        _inputReader.Interact -= HandleInteract;
        _inputReader.Interact -= _interactor.Interact;
        GameplayManager.Instance.OnStartGame -= OnStartGame;
        GameplayManager.Instance.OnEndGame -= OnEndGame;
        Emoter.OnEmoteSelected -= HandleEmote;
        _togglePlayerInputEvent.OnEventRaised -= HandleToggleInput;
    }

    private void OnEndGame(ulong arg0)
    {
        if (NetworkManager.Singleton.LocalClientId != arg0) return;
        _inputReader.Interact += HandleInteract;
        CameraController.SwitchFirstPersonCameraMode(CameraMode.FirstPersonSitting);
    }

    private void OnStartGame(ulong arg0)
    {
        if (NetworkManager.Singleton.LocalClientId != arg0) return;
        _inputReader.Interact -= HandleInteract;
        CameraController.SwitchFirstPersonCameraMode(CameraMode.FisrtPersonPlaying);
    }

    private void HandleMove(Vector2 arg0)
    {
        MoveDirection = new Vector3(arg0.x, 0, arg0.y).normalized;
        if (_currentState == _attackState || _currentState == _sitState || _currentState == _jumpState)
            return;
        if (MoveDirection == Vector3.zero)
        {
            ToState(_idleState);
        }
        else
        {
            ToState(_moveState);
        }
    }

    private void HandleAttack()
    {
        if (_currentState != _moveState && _currentState != _idleState)
        {
            return;
        }
        ToState(_attackState);
    }

    private void HandleInteract()
    {
        if (_currentState == _sitState)
            ToState(_idleState);
    }

    private void HandleEmote(int emoteIndex)
    {
        _emoteState.Select(emoteIndex);
        if (_currentState == _idleState)
        {
            ToState(_emoteState);
        }
    }

    private void HandleToggleInput(bool isActive)
    {
        _inputReader.ToggleInput(isActive);
        _canJump = isActive;
    }

    void Update()
    {
        if (!IsOwner)
            return;
        PreviousMovement = Movement;
        if (_currentState == _sitState)
            return;
        ApplyGravity();
        _currentState.OnTick();
        CharacterController.Move(Movement * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (CharacterController.isGrounded)
        {
            Movement.y = -1f;
            if (Keyboard.current.spaceKey.wasPressedThisFrame && _canJump)
            {
                ToState(_jumpState);
                return;
            }
            if (_currentState == _jumpState && CharacterController.velocity.y <= 0)
            {
                if (MoveDirection == Vector3.zero)
                    ToState(_idleState);
                else
                    ToState(_moveState);
            }
        }
        else
        {
            float newY = Movement.y + _gravity * Time.deltaTime;
            Movement.y = (PreviousMovement.y + newY) * 0.5f;
        }
    }

    private void ToState(IState newState)
    {
        if (_currentState == newState) return;
        _currentState.OnExit();
        _currentState = newState;
        _currentState.OnEnter();
    }

    public void Sit(Seat seat)
    {
        _sitState.With(seat);

        ToState(_sitState);
    }

    // called by animation event
    public void BackToIdle() => ToState(_idleState);

    public void OwnerActivateInput()
    {
        if (!IsOwner)
            return;

        _canJump = true;
        _inputReader.Move += HandleMove;
        _inputReader.Attack += HandleAttack;
        Emoter.OnEmoteSelected += HandleEmote;
    }

    public void DeactivateInput()
    {
        _canJump = false;
        _inputReader.Move -= HandleMove;
        _inputReader.Attack -= HandleAttack;
        Emoter.OnEmoteSelected -= HandleEmote;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Constant.FINISH_TAG))
        {
            Respawn();
        }
    }
}