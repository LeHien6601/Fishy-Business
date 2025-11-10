using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Interactor _interactor;
    [SerializeField] private InputReaderSO _inputReader;
    [SerializeField] private Transform _headBone;
    [SerializeField] private TransformEventChannelSO _headBoneTransformChannel;
    [SerializeField] private TransformEventChannelSO _targetTransformChannel;

    [SerializeField] private float _moveSpeed = 5f;
    public NavMeshAgent Agent;
    public Vector3 MoveDirection { get; private set; }
    private IState _currentState;
    private IdleState _idleState;
    private MoveState _moveState;
    private AttackState _attackState;
    private SitState _sitState;
    public bool CanInteract { get => _interactor.enabled; set => _interactor.enabled = value; }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            _interactor.enabled = false;
            // Agent.enabled = false;
            return;
        }
        base.OnNetworkSpawn();
        Animator animator = GetComponent<Animator>(); 
        _idleState = new IdleState(animator);
        _moveState = new MoveState(this, animator, _moveSpeed);
        _attackState = new AttackState(animator);
        _sitState = new SitState(this, animator);
        _currentState = _idleState;

        _inputReader.Move += HandleMove;
        _inputReader.Attack += HandleAttack;
        _inputReader.Interact += HandleInteract;
        GameplayManager.Instance.OnStartGame += OnStartGame;
        GameplayManager.Instance.OnEndGame += OnEndGame;

        _targetTransformChannel.RaiseEvent(transform);
        _headBoneTransformChannel.RaiseEvent(_headBone);
        CameraController.SwitchCamMode(CameraMode.ThirdPerson);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;
        base.OnNetworkDespawn();
        _inputReader.Move -= HandleMove;
        _inputReader.Attack -= HandleAttack;
        _inputReader.Interact -= HandleInteract;

        GameplayManager.Instance.OnStartGame -= OnStartGame;
        GameplayManager.Instance.OnEndGame -= OnEndGame;
    }

    private void OnEndGame(ulong arg0)
    {
        if (NetworkManager.Singleton.LocalClientId != arg0) return;
        _inputReader.Interact += HandleInteract;
    }

    private void OnStartGame(ulong arg0)
    {
        if (NetworkManager.Singleton.LocalClientId != arg0) return;
        _inputReader.Interact -= HandleInteract;
    }

    private void HandleMove(Vector2 arg0)
    {
        if (_currentState == _attackState || _currentState == _sitState)
            return;
        MoveDirection = new Vector3(arg0.x, 0, arg0.y).normalized;
        Debug.Log("Move");
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

    void Update()
    {
        if (!IsOwner)
            return;
        _currentState.OnTick();
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
    private void BackToIdle() => ToState(_idleState);
}