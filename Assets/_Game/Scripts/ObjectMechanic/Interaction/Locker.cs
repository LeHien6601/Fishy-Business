using System;
using UnityEngine;

public class Locker : MonoBehaviour, IInteractable
{
    [SerializeField] private InputReaderSO _inputReaderSO;
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
        //start changing outift
        _player = actor;
        UIManager.Instance.ShowUI(EUIState.CustomPlayer);
        CameraController.SwitchCamMode(CameraMode.CustomPlayer);
        actor.DeactivateInput();
        SwapInput();
        // @@TODO: UI, Camera
    }

    void OnDestroy()
    {
        _inputReaderSO.Move -= HandleInput;
        _inputReaderSO.Interact -= Exit;
    }
}