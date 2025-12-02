using UnityEngine;

public class JumpState : IState
{
    protected static readonly int _animHash = Animator.StringToHash("Jump");
    protected Animator _animator;
    protected PlayerController _host;
    private readonly float _defaultJumpVelocityX = 6f;
    private readonly float _initJumpVelocityY = 20f;
    private readonly float _airAcceleration = 4f;
    private readonly float _rotationSpeed = 500f;

    private Vector3 _currentHorizontalMomentum;
    public JumpState(PlayerController host, Animator animator, float moveSpeed = 5f, float initJumpVelocityY = 20f)
    {
        _host = host;
        _animator = animator;
        _defaultJumpVelocityX = moveSpeed;
        _initJumpVelocityY = initJumpVelocityY;
    }
    public virtual void OnEnter()
    {
        _animator.Play(_animHash);
        _host.Movement = _host.PreviousMovement + Vector3.up * _initJumpVelocityY;
        _currentHorizontalMomentum = _host.MoveDirection * _defaultJumpVelocityX;
        // _host.Movement.y = _initJumpVelocity;
    }

    public virtual void OnExit()
    {
    }

    public virtual void OnTick()
    {
        Vector3 targetDirection; ;
        if (_host.MoveDirection == Vector3.zero)
        {
            float decayTargetSpeed = _defaultJumpVelocityX * 0.5f;
            _currentHorizontalMomentum = Vector3.Lerp(_currentHorizontalMomentum, _currentHorizontalMomentum.normalized * decayTargetSpeed, _airAcceleration * Time.deltaTime);
        }
        else
        {
            float _targetAngle = Mathf.Atan2(_host.MoveDirection.x, _host.MoveDirection.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            Quaternion desiredRotation = Quaternion.Euler(0.0f, _targetAngle, 0.0f);
            _host.transform.rotation = Quaternion.RotateTowards(_host.transform.rotation, desiredRotation, _rotationSpeed * Time.deltaTime);
            targetDirection = desiredRotation * Vector3.forward;

            _currentHorizontalMomentum = targetDirection * _defaultJumpVelocityX;
        }
        _currentHorizontalMomentum.y = _host.Movement.y;
        _host.Movement = _currentHorizontalMomentum;
    }
}