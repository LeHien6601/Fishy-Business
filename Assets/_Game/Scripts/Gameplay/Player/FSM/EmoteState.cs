using UnityEngine;

public class EmoteState : IState
{
    protected Animator _animator;
    protected PlayerController _host;
    protected static readonly int _animHash = Animator.StringToHash("Emote");
    public EmoteState(PlayerController host, Animator animator)
    {
        _host = host;
        _animator = animator;
    }

    public void Select(int index)
    {
        _animator.SetFloat("Emote", index);
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