using UnityEngine;

public class SitState : IState
{
    private readonly PlayerController _host;
    private readonly Animator _animator;
    private static readonly int _animHash = Animator.StringToHash("Sit");
    private Seat _seat;

    public SitState(PlayerController host, Animator animator)
    {
        _host = host;
        _animator = animator;
    }

    public void With(Seat seat)
    {
        _seat = seat;
    }

    public virtual void OnEnter()
    {
        _host.Agent.enabled = false;
        _host.CanInteract = false;
        _seat.OnEnterSeat();
        _animator.Play(_animHash);
        _animator.transform.SetPositionAndRotation(_seat.SitPosition(), _seat.SitRotation());
        _host.transform.SetParent(_seat.transform);
    }

    public virtual void OnExit()
    {
        _seat.OnExitSeat();
        _host.Agent.enabled = true;
        _host.CanInteract = true;
        _host.transform.SetParent(null);
    }

    public virtual void OnTick()
    {
    }
}