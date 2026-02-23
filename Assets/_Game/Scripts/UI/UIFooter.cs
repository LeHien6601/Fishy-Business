using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class UIFooter : UIView
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _playerNameTMP;
    [SerializeField] private Image _playerIconImage;
    [SerializeField] private Button[] _playerInfoButtons;
    [SerializeField] private Button _globalSelfMuteButton;
    [SerializeField] private Image _globalSelfMuteIndicator;
    [SerializeField] private Image _micImage;
    [SerializeField] private Color _normalColor = Color.black;
    [SerializeField] private Color _speakingColor = Color.green;
    [Header("Event")]
    [SerializeField] private VoiceActivityEventChannelSO _voiceActivityEvent;
    void OnEnable()
    {
        _playerNameTMP.text = PlayerInfoManager.Instance.PlayerName;
        PlayerInfoManager.Instance.OnChangedPlayerInfo += HandleUpdatedPlayerInfo;
        foreach (var button in _playerInfoButtons)
        {
            button.onClick.AddListener(HandleClickPlayerInfo);
        }
        _globalSelfMuteButton.onClick.AddListener(HandleToggleGlobalSelfMute);
        _globalSelfMuteIndicator.gameObject.SetActive(VivoxManager.Instance.IsGlobalSelfMute);
        _voiceActivityEvent.OnEventRaised += HandleUpdatedVoiceActivity;
        UpdateUI();
    }
    void OnDisable()
    {
        PlayerInfoManager.Instance.OnChangedPlayerInfo -= HandleUpdatedPlayerInfo;
        foreach (var button in _playerInfoButtons)
        {
            button.onClick.RemoveListener(HandleClickPlayerInfo);
        }
        _globalSelfMuteButton.onClick.RemoveListener(HandleToggleGlobalSelfMute);
        _voiceActivityEvent.OnEventRaised -= HandleUpdatedVoiceActivity;
    }
    private void UpdateUI()
    {
        _playerNameTMP.text = PlayerInfoManager.Instance.PlayerName;
        _micImage.color = _normalColor;
        Sprite iconSprite = GameConfig.Instance.GetPlayerIconById(PlayerInfoManager.Instance.PlayerIconId);
        if (iconSprite != null)
        {
            _playerIconImage.sprite = iconSprite;
        }
        else
        {
            Debug.LogWarning($"Icon sprite for ID {PlayerInfoManager.Instance.PlayerIconId} not found.");
        }
    }
    private void HandleClickPlayerInfo()
    {
        Debug.Log("Footer: Player Info Button Clicked");
        SoundManager.Play2D(SoundType.ButtonClick);
        UIManager.Instance.ShowUI(EUIState.PlayerInfo);
    }
    private void HandleUpdatedPlayerInfo(PlayerInfoManager.ChangedPlayerInfoEventArgs args)
    {
        _playerNameTMP.text = args.NewPlayerName;
        Sprite iconSprite = GameConfig.Instance.GetPlayerIconById(args.NewPlayerIconId);
        if (iconSprite != null)
        {
            _playerIconImage.sprite = iconSprite;
        }
        else
        {
            Debug.LogWarning($"Icon sprite for ID {args.NewPlayerIconId} not found.");
        }
    }
    private void HandleToggleGlobalSelfMute()
    {
        VivoxManager.Instance.IsGlobalSelfMute = !VivoxManager.Instance.IsGlobalSelfMute;
        _globalSelfMuteIndicator.gameObject.SetActive(VivoxManager.Instance.IsGlobalSelfMute);
    }
    private void HandleUpdatedVoiceActivity(string playerId, bool isSpeaking)
    {
        if (playerId != AuthenticationService.Instance.PlayerId) return;
        _micImage.color = isSpeaking ? _speakingColor : _normalColor;
    }
}
