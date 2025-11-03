
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
        // ReparentHandler.Instance.RequestReparentServerRpc(_host.NetworkObjectId, _seat.NetworkObjectId);
        _host.Agent.enabled = false;
        _host.CanInteract = false;
        _animator.Play(_animHash);
        _animator.transform.SetPositionAndRotation(_seat.SitPosition(), _seat.SitRotation());
        if (_seat.CompareTag(Constant.GAME_SEAT_TAG)) CameraController.SwitchCamMode(CameraMode.FirstPerson);
    }

    public virtual void OnExit()
    {
        _seat.OnExitSeat();
        _host.Agent.enabled = true;
        _host.CanInteract = true;
        // _host.transform.SetParent(null);
        // ReparentHandler.Instance.RequestReparentServerRpc(_host.NetworkObjectId);
        CameraController.SwitchCamMode(CameraMode.ThirdPerson);
    }

    public virtual void OnTick()
    {
    }
}