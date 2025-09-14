using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _3rdPersonCamera;

    [Header("Listen to:")]
    [SerializeField] private TransformEventChannelSO _targetTransformChannel;

    void OnEnable()
    {
        _targetTransformChannel.OnEventRaised += TrackTarget;
    }

    void OnDisable()
    {
        _targetTransformChannel.OnEventRaised -= TrackTarget;
    }

    private void TrackTarget(Transform target)
    {
        _3rdPersonCamera.Follow = target;
    }
}