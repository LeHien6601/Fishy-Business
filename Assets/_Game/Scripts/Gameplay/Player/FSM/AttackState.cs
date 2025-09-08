using UnityEngine;

public class AttackState : IState
{
    protected Animator _animator;
    protected static readonly int _animHash = Animator.StringToHash("Attack");
    public AttackState(Animator animator)
    {
        _animator = animator;
    }
    public virtual void OnEnter()
    {
        _animator.Play(_animHash);
        _animator.applyRootMotion = true;
    }

    public virtual void OnExit()
    {
        _animator.applyRootMotion = false;
    }

    public virtual void OnTick()
    {
    }
}