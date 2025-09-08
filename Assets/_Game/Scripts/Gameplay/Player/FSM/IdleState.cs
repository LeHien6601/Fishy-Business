using UnityEngine;

public class IdleState : IState
{
    protected Animator _animator;
    protected static readonly int _animHash = Animator.StringToHash("Idle");
    public IdleState(Animator animator)
    {
        _animator = animator;
    }
    public virtual void OnEnter()
    {
        _animator.Play(_animHash);
    }

    public virtual void OnExit()
    {
    }

    public virtual void OnTick()
    {
    }
}