using UnityEngine;

public class JumpState : IState
{
    protected static readonly int _animHash = Animator.StringToHash("Jump");
    protected Animator _animator;
    protected PlayerController _host;
    float _initJumpVelocity = 20f;

    public JumpState(PlayerController host, Animator animator, float moveSpeed = 5f)
    {
        _host = host;
        _animator = animator;
    }
    public virtual void OnEnter()
    {
        _animator.Play(_animHash);
        // _host.Movement = _host.PreviousMovement + Vector3.up * _initJumpVelocity;
        _host.Movement.y = _initJumpVelocity;
    }

    public virtual void OnExit()
    {
    }

    public virtual void OnTick()
    {
        float _targetRotation = Mathf.Atan2(_host.MoveDirection.x, _host.MoveDirection.z) * Mathf.Rad2Deg +
       Camera.main.transform.eulerAngles.y;

        // rotate to face input direction relative to camera position
        _host.transform.rotation = Quaternion.Euler(0.0f, _targetRotation, 0.0f);

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        float preservedY = _host.Movement.y;
        _host.Movement = 6 * targetDirection;
        _host.Movement.y = preservedY;
    }
}