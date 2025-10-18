using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _3rdPersonCamera;
    [SerializeField] private CinemachineCamera _1stPersonCamera;

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