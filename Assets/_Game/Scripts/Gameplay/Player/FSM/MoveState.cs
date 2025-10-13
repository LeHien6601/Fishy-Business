using Unity.Netcode.Components;
using UnityEngine;

public class MoveState : IState
{
    private readonly Animator _animator;
    private readonly PlayerController _host;
    private readonly float _moveSpeed;
    private static readonly int _animBoolHash = Animator.StringToHash("IsMoving");
    private readonly Camera mainCamera;
    public MoveState(PlayerController host, Animator animator, float moveSpeed)
    {
        _host = host;
        _animator = animator;
        _moveSpeed = moveSpeed;
        mainCamera = Camera.main;
    }


    public void OnEnter()
    {
        // _host.DustVfx.Play();
        _animator.SetBool(_animBoolHash, true);
    }

    public void OnExit()
    {
        _animator.SetBool(_animBoolHash, false);
        // _host.DustVfx.Stop();
    }

    public void OnTick()
    {
        MoveWithCameraDirection();
        // _host.transform.forward = _host.MoveDirection;
        // _host.transform.position = Vector3.MoveTowards(_host.transform.position, _host.transform.position + _host.MoveDirection, Time.deltaTime * _moveSpeed);
    }




    /// <summary>
    /// top down movement
    /// </summary>
    private void Move()
    {
        _host.transform.forward = -_host.MoveDirection;
        _host.transform.position += _moveSpeed * Time.deltaTime * -_host.MoveDirection;
    }

    /// <summary>
    /// third person movement
    /// </summary>
    private void MoveWithCameraDirection()
    {
        float _targetRotation = Mathf.Atan2(_host.MoveDirection.x, _host.MoveDirection.z) * Mathf.Rad2Deg +
               Camera.main.transform.eulerAngles.y;

        // rotate to face input direction relative to camera position
        _host.transform.rotation = Quaternion.Euler(0.0f, _targetRotation, 0.0f);

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        _host.transform.position += _moveSpeed * Time.deltaTime * targetDirection;
    }
}