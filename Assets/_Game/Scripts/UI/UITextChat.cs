using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WebSocketSharp;

public class UITextChat : UIView
{
    [SerializeField] private Button _toggleBTN;
    [SerializeField] private RectTransform _textBoxContainer;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _sendBTN;
    [SerializeField] private RectTransform _notiRect;
    [SerializeField] private UITextChatElement _textChatElementPrefab;
    [SerializeField] private RectTransform _messageContainer;
    [SerializeField] private VerticalLayoutGroup _verticalLayoutGroup;
    private CursorLockMode _lastCursorMode;
    private List<UITextChatElement> _elements = new();

    void OnEnable()
    {
        _toggleBTN.onClick.AddListener(OnToggleClicked);
        _sendBTN.onClick.AddListener(OnSendClicked);
        SetActiveTextBox(false);
        SetActiveNotiRect(false);
        VivoxManager.Instance.OnTextMessageReceived += DisplayNewMessage;
    }

    private void DisplayNewMessage(VivoxMessage message)
    {
        if (_textBoxContainer.gameObject.activeSelf)
        {
            LoadHistory();
        }
        else
        {
            SetActiveNotiRect(true);
        }
    }

    void OnDisable()
    {
        _toggleBTN.onClick.RemoveListener(OnToggleClicked);
        _sendBTN.onClick.RemoveListener(OnSendClicked);
    }

    void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame) OnToggleClicked();
    }

    private async void OnSendClicked() 
    {
        if (_inputField.text.IsNullOrEmpty()) return;
        await VivoxManager.Instance.SendTextMessageAsync(_inputField.text);
        _inputField.text = "";
        _inputField.ActivateInputField();
    }

    private void OnToggleClicked()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        bool turnOn = !_textBoxContainer.gameObject.activeSelf;
        SetActiveTextBox(turnOn);
        if (turnOn) {
            _lastCursorMode = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            LoadHistory();
        }
        else
        {
            Cursor.lockState = _lastCursorMode;
        }
        EventSystem.current.SetSelectedGameObject(null);
    }
    private async void LoadHistory()
    {
        SetActiveNotiRect(false);

        var history = await VivoxManager.Instance.GetHistoryAsync();
        var tempHistory = new List<VivoxMessage>();
        if (history != null)
        {
            foreach (var msg in history)
            {
                tempHistory.Add(msg);
            }
        }
        tempHistory = tempHistory.OrderBy(msg => msg.ReceivedTime).ToList();
        int count = 0;
        if (tempHistory != null)
        {
            foreach (var msg in tempHistory)
            {
                AddMessageToUI(msg, count);
                count++;
            }
        }
        for (int i = 0; i < _elements.Count; i++)
        {
            _elements[i].gameObject.SetActive(i <= count);
        }
    }
    private void AddMessageToUI(VivoxMessage msg, int count)
    {
        UITextChatElement element;

        if (count >= _elements.Count)
        {
            element = Instantiate(_textChatElementPrefab, _messageContainer);
            _elements.Add(element);
        }
        else
        {
            element = _elements[count];
        }
        // Setup the element
        string timestamp = msg.ReceivedTime.ToLocalTime().ToString("HH:mm");
        var playerInfo = LobbyManager.Instance.GetPlayerInfoFromPlayerId(msg.SenderPlayerId);
        string finalName = playerInfo.Found ? playerInfo.Name : msg.SenderDisplayName;
        element.SetText($"<color=#888888>[{timestamp}]</color> <b>{finalName}:</b> {msg.MessageText}");

        Canvas.ForceUpdateCanvases();
        StartCoroutine(ScrollToBottom());
        StartCoroutine(UpdateVerticleLayoutGroup());
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
    
    private void SetActiveNotiRect(bool isActive)
    {
        if (!isActive)
        {
            _notiRect.gameObject.SetActive(false);
        }
        else
        {
            _notiRect.gameObject.SetActive(true);
            _notiRect.localScale = Vector3.zero;
            _notiRect.DOScale(1, 0.5f).SetEase(Ease.OutBack);
        }
    }
    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        _scrollRect.verticalNormalizedPosition = 0f;
    }
    private IEnumerator UpdateVerticleLayoutGroup()
    {
        _verticalLayoutGroup.enabled = false;
        yield return new WaitForEndOfFrame();
        _verticalLayoutGroup.enabled = true;
        yield return new WaitForEndOfFrame();
        _verticalLayoutGroup.enabled = false;
        yield return new WaitForEndOfFrame();
        _verticalLayoutGroup.enabled = true;
    }
}