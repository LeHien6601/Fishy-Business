using System.Collections.Generic;
using System.Threading.Tasks;
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
    [SerializeField] private Button _saveBtn;
    [SerializeField] private TMP_InputField _lobbyNameInput;
    [SerializeField] private TextMeshProUGUI _noteTMP;
    [SerializeField] private RectTransform _memberRect;
    [SerializeField] private TextMeshProUGUI _titleTMP;

    [Header("Game Data")]
    [SerializeField] private RectTransform _dataRect;
    [SerializeField] private Button _leftBtn;
    [SerializeField] private Button _rightBtn;
    [SerializeField] private List<Button> _gameModeBtns;
    [SerializeField] private List<RectTransform> _gameModeIndicators;
    [SerializeField] private Color _normalColor;
    [SerializeField] private Color _selectedColor;
    [SerializeField] private List<UIValueSlider> _sliders;
    [SerializeField] private List<RectTransform> _sliderContainers;

    [Header("Properties")]
    [SerializeField] private string _tooShortNameNote = "Lobby name is too short!\nMin length is 3 characters.";
    [SerializeField] private string _tooLongNameNote = "Lobby name is too long!\nMax length is 14 characters.";
    [SerializeField] private string _saveInfoSuccessNote = "Lobby data saved successfully.";
    private const int MIN_NAME_LENGTH = 3;
    private const int MAX_NAME_LENGTH = 14;
    private int _currentGameMode = 0;
    private GameData _gameData;
    void OnEnable()
    {
        _lobbyNameInput.interactable = false;
        _noteTMP.gameObject.SetActive(false);
        _editNameBtn.gameObject.SetActive(LobbyManager.Instance.isHost);
        _titleTMP.text = "MEMBERS";
        if (!LobbyManager.Instance.isHost)
        {
            _leftBtn.gameObject.SetActive(false);
            _rightBtn.gameObject.SetActive(false);
        }
        else
        {
            _gameData = JsonUtility.FromJson<GameData>(LobbyManager.Instance.currentLobby.Data[Constant.KEY_GAME_MODE_DATA].Value);
            _leftBtn.gameObject.SetActive(false);
            _rightBtn.gameObject.SetActive(true);
        }
        _memberRect.gameObject.SetActive(true);
        _dataRect.gameObject.SetActive(false);
        UpdateUI();
        LobbyManager.Instance.OnUpdatedCurrentLobby += HandleUpdateLobby;
        _backButton.onClick.AddListener(HandleClickBack);
        _resetButton.onClick.AddListener(HandleClickReset);
        _editNameBtn.onClick.AddListener(HandleClickEditName);
        _saveBtn.onClick.AddListener(HandleClickSave);

        _leftBtn.onClick.AddListener(() => HandleClickChangeTab(true));
        _rightBtn.onClick.AddListener(() => HandleClickChangeTab(false));

        _gameModeBtns[0].onClick.AddListener(() => ChangeGameMode(0));
        _gameModeBtns[1].onClick.AddListener(() => ChangeGameMode(1));
    }
    void OnDisable()
    {
        LobbyManager.Instance.OnUpdatedCurrentLobby -= HandleUpdateLobby;
        _backButton.onClick.RemoveListener(HandleClickBack);
        _resetButton.onClick.RemoveListener(HandleClickReset);
        _editNameBtn.onClick.RemoveListener(HandleClickEditName);
        _saveBtn.onClick.RemoveListener(HandleClickSave);

        _leftBtn.onClick.RemoveAllListeners();
        _rightBtn.onClick.RemoveAllListeners();

        _gameModeBtns[0].onClick.RemoveAllListeners();
        _gameModeBtns[1].onClick.RemoveAllListeners();
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
        int currentNumOfPlayer = lobby.Players.Count;
        string mineId = AuthenticationService.Instance.PlayerId;
        for (int i = 0; i < Constant.MAX_PLAYERS; i++)
        {
            if (i < currentNumOfPlayer)
            {
                _uiMembers[i].SetMemberData(
                    mineId == lobby.Players[i].Id,
                    lobby.Players[i].Data[Constant.KEY_PLAYER_NAME].Value,
                    lobby.Players[i].Data[Constant.KEY_PLAYER_ICON_ID].Value != null ?
                        GameConfig.Instance.GetPlayerIconById(
                            int.Parse(lobby.Players[i].Data[Constant.KEY_PLAYER_ICON_ID].Value)) : null,
                    lobby.Players[i].Id);
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
    }
    private void HandleClickReset()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        Hide();
        GameplayManager.Instance.TriggerResetGame();
    }
    private void HandleClickEditName()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        _lobbyNameInput.interactable = true;
        _editNameBtn.gameObject.SetActive(false);
    }
    private async void HandleClickSave()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        string oldName = LobbyManager.Instance.currentLobby.Name;
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
            _editNameBtn.gameObject.SetActive(true);
        }
        else
        {
            ShowNote("Failed to update lobby name. Please try again.");
        }
        if (oldName != newName) await Task.Delay(500);
        SaveLobbyData();
    }
    private async void ShowNote(string note)
    {
        _noteTMP.text = note;
        _noteTMP.gameObject.SetActive(true);
        _noteTMP.rectTransform.localScale = Vector3.zero;
        _noteTMP.rectTransform.DOScale(Vector2.one, 0.3f).SetEase(Ease.OutBack);
        await Task.Delay(2000);
        _noteTMP.rectTransform.DOScale(Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            _noteTMP.gameObject.SetActive(false);
        });
    }

    private void HandleClickChangeTab(bool isLeft)
    {
        if (isLeft)
        {
            OpenMemberTab();
        }
        else
        {
            OpenGameDataTab();
        }
    }
    private void OpenMemberTab()
    {
        _titleTMP.text = "MEMBERS";
        _leftBtn.gameObject.SetActive(false);
        _rightBtn.gameObject.SetActive(true);
        _dataRect.gameObject.SetActive(false);
        _memberRect.gameObject.SetActive(true);

    }
    private void OpenGameDataTab()
    {
        _titleTMP.text = "GAMEPLAY CONFIG";
        _leftBtn.gameObject.SetActive(true);
        _rightBtn.gameObject.SetActive(false);
        _dataRect.gameObject.SetActive(true);
        _memberRect.gameObject.SetActive(false);
        ChangeGameMode(_gameData.GameModeIndex);
    }
    private void ChangeGameMode(int index)
    {
        Debug.Log($"Current game mode {index}");
        _currentGameMode = index;
        for (int i = 0; i < 2; i++)
        {
            _gameModeBtns[i].image.color = (i == _currentGameMode) ? _selectedColor : _normalColor;
            _gameModeIndicators[i].gameObject.SetActive(i == _currentGameMode);
        }
        _sliders[0].SetValue(_gameData.TurnInterval);
        if (index == 1)
        {
            _sliderContainers[0].gameObject.SetActive(true);
            _sliderContainers[1].gameObject.SetActive(true);
            _sliderContainers[2].gameObject.SetActive(true);
            _sliders[1].SetValue(_gameData.VotingInterval);
            _sliders[2].SetValue(_gameData.DayDiscussionInverval);
        }
        else
        {
            _sliderContainers[0].gameObject.SetActive(true);
            _sliderContainers[1].gameObject.SetActive(false);
            _sliderContainers[2].gameObject.SetActive(false);
        }
    }
    private async void SaveLobbyData()
    {
        _gameData.GameModeIndex = _currentGameMode;
        _gameData.TurnInterval = _sliders[0].GetValue();
        _gameData.VotingInterval = (_currentGameMode == 1) ? _sliders[1].GetValue() : Constant.DEFAULT_VOTING_INTERVAL;
        _gameData.DayDiscussionInverval = (_currentGameMode == 1) ? _sliders[2].GetValue() : Constant.DEFAULT_DISCUSSION_INTERVAL;
        bool success = await LobbyManager.Instance.UpdateLobbyGameDataAsync(JsonUtility.ToJson(_gameData));
        if (success)
        {
            ShowNote(_saveInfoSuccessNote);
        }
        else
        {
            ShowNote("Failed to update lobby data. Please try again.");
        }
    }
}
