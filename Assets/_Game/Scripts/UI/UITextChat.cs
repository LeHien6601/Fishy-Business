using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UITextChat : UIView
{
    [SerializeField] private Button _toggleBTN;
    [SerializeField] private RectTransform _textBoxContainer;
    private CursorLockMode _lastCursorMode;

    void OnEnable()
    {
        _toggleBTN.onClick.AddListener(HandleClickToggle);
        SetActiveTextBox(false);
    }
    void OnDisable()
    {
        _toggleBTN.onClick.RemoveListener(HandleClickToggle);
    }

    void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame) HandleClickToggle();
    }

    private void HandleClickToggle()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        bool turnOn = !_textBoxContainer.gameObject.activeSelf;
        SetActiveTextBox(turnOn);
        if (turnOn) {
            _lastCursorMode = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = _lastCursorMode;
        }
        EventSystem.current.SetSelectedGameObject(null);
    }
    private void SetActiveTextBox(bool isActive)
    {
        if (!isActive)
        {
            _textBoxContainer.gameObject.SetActive(false);
        }
        else
        {
            _textBoxContainer.gameObject.SetActive(true);
            _textBoxContainer.localScale = Vector3.zero;
            _textBoxContainer.DOScale(1, 0.5f).SetEase(Ease.OutBack);
        }
    }
}