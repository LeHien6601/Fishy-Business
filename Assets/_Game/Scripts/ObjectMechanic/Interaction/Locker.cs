using System;
using UnityEngine;

public class Locker : MonoBehaviour, IInteractable
{
    [SerializeField] private InputReaderSO _inputReaderSO;
    [SerializeField] private Transform _cameraTransform;
    private PlayerController _player;


    private void SwapInput()
    {
        _inputReaderSO.Move += HandleInput;
        _inputReaderSO.Interact += Exit;
    }

    private void Exit()
    {
        _player.OwnerActivateInput();
        _inputReaderSO.Move -= HandleInput;
        _inputReaderSO.Interact -= Exit;
        UIManager.Instance.HideUI(EUIState.CustomPlayer);
        CameraController.SwitchCamMode(CameraMode.ThirdPerson);
    }

    private void HandleInput(Vector2 arg0)
    {
        // hook to the OutfitSelector.ChangeOutfit
    }

    public void Interact(PlayerController actor)
    {
        _player = actor;
        UIManager.Instance.ShowUI(EUIState.CustomPlayer);
        CameraController.SetCustomCameraTransform(_cameraTransform.position, _cameraTransform.rotation);
        actor.DeactivateInput();
        SwapInput();
    }

    void OnDestroy()
    {
        _inputReaderSO.Move -= HandleInput;
        _inputReaderSO.Interact -= Exit;
    }
}