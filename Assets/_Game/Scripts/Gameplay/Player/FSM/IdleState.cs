using UnityEngine;

public class IdleState : IState
{
    protected Animator _animator;
    protected PlayerController _host;   
    protected static readonly int _animHash = Animator.StringToHash("Idle");
    public IdleState(PlayerController host, Animator animator)
    {
        _host = host;
        _animator = animator;
    }
    public virtual void OnEnter()
    {
        _animator.Play(_animHash);
        _host.Movement = Vector3.zero;
    }

    public virtual void OnExit()
    {
    }

    public virtual void OnTick()
    {
    }
}