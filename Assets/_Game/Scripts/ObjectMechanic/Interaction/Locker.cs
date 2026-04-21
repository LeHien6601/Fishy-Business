using System;
using DG.Tweening;
using UnityEngine;

public class Locker : MonoBehaviour, IInteractable
{
    [SerializeField] private InputReaderSO _inputReaderSO;
    [SerializeField] private Transform _cameraTransform;
    
    [SerializeField] private Transform _highlightArrow;
    private PlayerController _player;

    void OnEnable()
    {
        ShowHighlightArrow();
    }

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
        HideHighlightArrow();
    }
    private void ShowHighlightArrow()
    {
        _highlightArrow.gameObject.SetActive(true);
        _highlightArrow.DOKill();
        _highlightArrow.localPosition = new Vector3(0, 3f, 0);
        _highlightArrow.DOLocalMoveY(4f, 0.5f).SetEase(Ease.OutSine).SetLoops(-1, LoopType.Yoyo);
        _highlightArrow.DOLocalRotate(new Vector3(0, 180f, -90), 2f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
    }
    private void HideHighlightArrow()
    {
        _highlightArrow.gameObject.SetActive(false);
        _highlightArrow.DOKill();
    }
}