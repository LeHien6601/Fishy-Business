using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class UILobbyInfo : UIView
{
    [Header("References")]
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private List<UILobbyMember> _uiMembers = new();
    [SerializeField] private Button _editNameBtn;
    [SerializeField] private Button _saveNameBtn;
    [SerializeField] private TMP_InputField _lobbyNameInput;
    [SerializeField] private TextMeshProUGUI _noteTMP;

    [Header("Properties")]
    [SerializeField] private string _tooShortNameNote = "Lobby name is too short!\nMin length is 3 characters.";
    [SerializeField] private string _tooLongNameNote = "Lobby name is too long!\nMax length is 12 characters.";
    [SerializeField] private string _saveInfoSuccessNote = "Lobby name saved successfully.";
    private const int MIN_NAME_LENGTH = 3;
    private const int MAX_NAME_LENGTH = 12;
    void OnEnable()
    {
        _lobbyNameInput.interactable = false;
        _saveNameBtn.gameObject.SetActive(false);
        _noteTMP.gameObject.SetActive(false);
        _editNameBtn.gameObject.SetActive(LobbyManager.Instance.isHost);
        UpdateUI();
        LobbyManager.Instance.OnUpdatedCurrentLobby += HandleUpdateLobby;
        _backButton.onClick.AddListener(HandleClickBack);
        _resetButton.onClick.AddListener(HandleClickReset);
        _editNameBtn.onClick.AddListener(HandleClickEditName);
        _saveNameBtn.onClick.AddListener(HandleClickSaveName);
    }
    void OnDisable()
    {
        LobbyManager.Instance.OnUpdatedCurrentLobby -= HandleUpdateLobby;
        _backButton.onClick.RemoveListener(HandleClickBack);
        _resetButton.onClick.RemoveListener(HandleClickReset);
        _editNameBtn.onClick.RemoveListener(HandleClickEditName);
        _saveNameBtn.onClick.RemoveListener(HandleClickSaveName);
    }
    private void HandleUpdateLobby(LobbyManager.UpdateCurrentLobbyEventArgs args)
    {
        UpdateUI();
    }
    private void UpdateUI()
    {
        if (LobbyManager.Instance.currentLobby == null) return;
        Lobby lobby = LobbyManager.Instance.currentLobby;
        if (!_lobbyNameInput.interactable) _lobbyNameInput.text = lobby.Name;
        int currentNumOfPlayer = LobbyManager.Instance.currentLobby.Players.Count;
        string mineId = AuthenticationService.Instance.PlayerId;
        Lobby currentLobby = LobbyManager.Instance.currentLobby;
        for (int i = 0; i < Constant.MAX_PLAYERS; i++)
        {
            if (i < currentNumOfPlayer)
            {
                _uiMembers[i].SetMemberData(
                    mineId == currentLobby.Players[i].Id,
                    currentLobby.Players[i].Data[Constant.KEY_PLAYER_NAME].Value,
                    currentLobby.Players[i].Data[Constant.KEY_PLAYER_ICON_ID].Value != null ?
                        GameConfig.Instance.GetPlayerIconById(
                            int.Parse(currentLobby.Players[i].Data[Constant.KEY_PLAYER_ICON_ID].Value)) : null,
                    currentLobby.Players[i].Id);
            }
            else {
                _uiMembers[i].ResetMemberData();
            }
        }
        _resetButton.interactable = LobbyManager.Instance.isHost 
            && LobbyManager.Instance.currentLobby.Data[Constant.KEY_START_GAME].Value == "true";
    }
    private void HandleClickBack()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        Hide();
        // UIManager.Instance.ShowUI(EUIState.LobbyGameplay);
    }
    private void HandleClickReset()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        Hide();
        // UIManager.Instance.ShowUI(EUIState.LobbyGameplay);
        GameplayManager.Instance.TriggerResetGame();
    }
    private void HandleClickEditName()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        _lobbyNameInput.interactable = true;
        _saveNameBtn.gameObject.SetActive(true);
        _editNameBtn.gameObject.SetActive(false);
    }
    private async void HandleClickSaveName()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        string newName = _lobbyNameInput.text.Trim();
        if (newName.Length < MIN_NAME_LENGTH)
        {
            ShowNote(_tooShortNameNote);
            return;
        }
        if (newName.Length > MAX_NAME_LENGTH)
        {
            ShowNote(_tooLongNameNote);
            return;
        }
        bool success = await LobbyManager.Instance.UpdateLobbyNameAsync(newName);
        if (success)
        {
            ShowNote(_saveInfoSuccessNote);
            _lobbyNameInput.interactable = false;
            _saveNameBtn.gameObject.SetActive(false);
            _editNameBtn.gameObject.SetActive(true);
        }
        else
        {
            ShowNote("Failed to update lobby name. Please try again.");
        }
    }
    private async void ShowNote(string note)
    {
        _noteTMP.text = note;
        _noteTMP.gameObject.SetActive(true);
        _noteTMP.rectTransform.localScale = Vector3.zero;
        _noteTMP.rectTransform.DOScale(Vector2.one, 0.3f).SetEase(Ease.OutBack);
        await System.Threading.Tasks.Task.Delay(2000);
        _noteTMP.rectTransform.DOScale(Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            _noteTMP.gameObject.SetActive(false);
        });
    }
}
