using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRoundTable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _blockRect;
    [SerializeField] private List<Button> _gameModeBtns;
    [SerializeField] private List<RectTransform> _gameModeIndicators;
    [SerializeField] private Color _normalColor;
    [SerializeField] private Color _selectedColor;
    [SerializeField] private List<UIValueSlider> _sliders;
    [SerializeField] private List<RectTransform> _sliderContainers;
    [SerializeField] private TextMeshProUGUI _noteTMP;
    [SerializeField] private Button _saveBTN;
    [SerializeField] private Button _startBTN;
    [SerializeField] private Button _toggleDataContainerBTN;
    [SerializeField] private RectTransform _dataContainer;
    
    private GameData _gameData;
    private int _currentGameMode = 0;
    void Awake()
    {
        gameObject.SetActive(false);
    }
    void OnEnable()
    {
        UpdateUI();
        _blockRect.gameObject.SetActive(!LobbyManager.Instance.isHost);

        _gameModeBtns[0].onClick.AddListener(() => ChangeGameMode(0));
        _gameModeBtns[1].onClick.AddListener(() => ChangeGameMode(1));

        foreach (var slider in _sliders)
        {
            slider.Slider.onValueChanged.AddListener(HandleSliderValueChange);
        }
        _startBTN.onClick.AddListener(HandleClickStart);
        _saveBTN.onClick.AddListener(HandleClickSave);
        _toggleDataContainerBTN.onClick.AddListener(HandleClickToggle);
        
        LobbyManager.Instance.OnUpdatedCurrentLobby += HandleUpdateLobby;
    }
    void OnDisable()
    {
        _gameModeBtns[0].onClick.RemoveAllListeners();
        _gameModeBtns[1].onClick.RemoveAllListeners();

        foreach (var slider in _sliders)
        {
            slider.Slider.onValueChanged.RemoveAllListeners();
        }
        _startBTN.onClick.RemoveListener(HandleClickStart);
        _saveBTN.onClick.RemoveListener(HandleClickSave);
        _toggleDataContainerBTN.onClick.RemoveListener(HandleClickToggle);
        
        LobbyManager.Instance.OnUpdatedCurrentLobby -= HandleUpdateLobby;
    }

    private void ChangeGameMode(int index)
    {
        SoundManager.Play2D(SoundType.ButtonClick);
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
            ShowNote("Save game configuration success.");
        }
        else
        {
            ShowNote("Failed to save game configuration.");
        }
    }
    private void UpdateUI()
    {
        _gameData = JsonUtility.FromJson<GameData>(LobbyManager.Instance.currentLobby.Data[Constant.KEY_GAME_MODE_DATA].Value);
        ChangeGameMode(_gameData.GameModeIndex);
    }
    private void HandleUpdateLobby(LobbyManager.UpdateCurrentLobbyEventArgs args)
    {
        if (LobbyManager.Instance.isHost) return;
        UpdateUI();
    }
    private void HandleSliderValueChange(float value)
    {
        SoundManager.Play2D(SoundType.ButtonClick);
    }
    private void HandleClickSave()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        SaveLobbyData();
    }
    private void HandleClickStart()
    {
        if (!LobbyManager.Instance.isHost) return;
        SaveLobbyData();
    }
    private void HandleClickToggle()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        _dataContainer.gameObject.SetActive(!_dataContainer.gameObject.activeSelf);
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