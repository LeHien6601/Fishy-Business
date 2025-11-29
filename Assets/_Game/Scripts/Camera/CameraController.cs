using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _3rdPersonCamera;
    [SerializeField] private CinemachineCamera _1stPersonCamera;
    [SerializeField] private CinemachineCamera _customPlayerCamera;
    [SerializeField] private CinemachineCamera _sceneViewCamera;
    [SerializeField] private CinemachineInputAxisController _cinemachineInputAxisController;
    public static event UnityAction<CameraMode> OnCameraModeSwitched;
    private static CameraMode _cameraMode;
    private static CameraMode _previousMode;
    private Transform _headBoneTransform;

    [SerializeField] private Quaternion _headBoneOffset = Quaternion.Euler(0, 0, 0);
    [SerializeField] private float _yAxisMultiplier = 1f;
    [Header("Listen to:")]
    [SerializeField] private TransformEventChannelSO _targetTransformChannel;
    [SerializeField] private TransformEventChannelSO _headBoneTransformChannel;


    void OnEnable()
    {
        OnCameraModeSwitched += OnSwitchCamMode;
        _targetTransformChannel.OnEventRaised += TrackTarget;
        _headBoneTransformChannel.OnEventRaised += AssignHeadBone;
    }
    void OnDisable()
    {
        _targetTransformChannel.OnEventRaised -= TrackTarget;
        _headBoneTransformChannel.OnEventRaised -= AssignHeadBone;
        OnCameraModeSwitched -= OnSwitchCamMode;
    }

    private void AssignHeadBone(Transform arg0)
    {
        _headBoneTransform = arg0;
    }

    private void OnSwitchCamMode(CameraMode mode)
    {
        _previousMode = _cameraMode;
        _cameraMode = mode;
        ResetPriorities();
        switch (mode)
        {
            case CameraMode.ThirdPerson:
                _3rdPersonCamera.Priority = 10;
                _3rdPersonCamera.transform.rotation = _3rdPersonCamera.Follow.rotation;
                Cursor.lockState = CursorLockMode.Locked;
                break;
            case CameraMode.FirstPerson:
                _1stPersonCamera.Priority = 10;
                if (_1stPersonCamera.TryGetComponent<CinemachinePanTilt>(out var pan))
                {
                    pan.PanAxis.Value = pan.PanAxis.Center;
                    pan.TiltAxis.Value = pan.TiltAxis.Center;
                }
                _cinemachineInputAxisController.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                break;
            case CameraMode.FirstPersonWithFreeLook:
                _1stPersonCamera.Priority = 10;
                _cinemachineInputAxisController.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                break;
            case CameraMode.CustomPlayer:
                _customPlayerCamera.Priority = 10;
                Cursor.lockState = CursorLockMode.None;
                break;
            case CameraMode.SceneView:
                _sceneViewCamera.Priority = 10;
                Cursor.lockState = CursorLockMode.None;
                break;
        }
    }

    private void LateUpdate()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // swithc between 1st and 1stperson with free look
            if (_cameraMode == CameraMode.FirstPerson)
            {
                SwitchCamMode(CameraMode.FirstPersonWithFreeLook);
            }
            else if (_cameraMode == CameraMode.FirstPersonWithFreeLook)
            {
                SwitchCamMode(CameraMode.FirstPerson);
            }
        }
        // rotate the head bone:
        if (_headBoneTransform != null && _cameraMode == CameraMode.FirstPersonWithFreeLook)
        {
            _headBoneTransform.rotation = _1stPersonCamera.transform.rotation * _headBoneOffset;
            // handle y axis multiplier
            Vector3 euler = _headBoneTransform.localEulerAngles;
            euler.y = euler.y > 180 ? euler.y - 360 : euler.y;
            euler.y *= _yAxisMultiplier;
            _headBoneTransform.localEulerAngles = euler;
        }
    }

    private void ResetPriorities()
    {
        _3rdPersonCamera.Priority = 0;
        _1stPersonCamera.Priority = 0;
        _customPlayerCamera.Priority = 0;
        _sceneViewCamera.Priority = 0;
    }

    private void TrackTarget(Transform target)
    {
        _1stPersonCamera.Follow = target;
        _3rdPersonCamera.Follow = target;
    }

    public static void ToPreviousMode()
    {
        SwitchCamMode(_previousMode);
    }

    public static void SwitchCamMode(CameraMode mode)
    {
        OnCameraModeSwitched?.Invoke(mode);
    }
}

public enum CameraMode
{
    ThirdPerson,
    FirstPerson,
    FirstPersonWithFreeLook,
    CustomPlayer,
    SceneView,
}