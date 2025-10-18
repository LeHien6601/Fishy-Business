using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _3rdPersonCamera;
    [SerializeField] private CinemachineCamera _1stPersonCamera;

    [SerializeField] private CinemachineInputAxisController _cinemachineInputAxisController;
    private static event UnityAction<CameraMode> OnCameraModeSwitched;

    [Header("Listen to:")]
    [SerializeField] private TransformEventChannelSO _targetTransformChannel;

    void OnEnable()
    {
        OnCameraModeSwitched += OnSwitchCamMode;
        _targetTransformChannel.OnEventRaised += TrackTarget;
    }

    private void OnSwitchCamMode(CameraMode mode)
    {
        if (mode == CameraMode.ThirdPerson)
        {
            _1stPersonCamera.gameObject.SetActive(false);
            _3rdPersonCamera.gameObject.SetActive(true);
            _3rdPersonCamera.transform.rotation = _3rdPersonCamera.Follow.rotation;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (mode == CameraMode.FirstPerson)
        {
            _3rdPersonCamera.gameObject.SetActive(false);
            _1stPersonCamera.gameObject.SetActive(true);
            if (_1stPersonCamera.TryGetComponent<CinemachinePanTilt>(out var pan))
            {
                pan.PanAxis.Value = 0;
                pan.TiltAxis.Value = 0;
            }
            _1stPersonCamera.transform.rotation = _1stPersonCamera.Follow.rotation;
            _cinemachineInputAxisController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            _cinemachineInputAxisController.enabled = !_cinemachineInputAxisController.enabled;
            Cursor.lockState = _cinemachineInputAxisController.enabled ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }

    void OnDisable()
    {
        _targetTransformChannel.OnEventRaised -= TrackTarget;
    }

    private void TrackTarget(Transform target)
    {
        _1stPersonCamera.Follow = target;
        _3rdPersonCamera.Follow = target;
    }

    public static void SwitchCamMode(CameraMode mode)
    {
        OnCameraModeSwitched?.Invoke(mode);
    }
}

public enum CameraMode
{
    ThirdPerson,
    FirstPerson
}