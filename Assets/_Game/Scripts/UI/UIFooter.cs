using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFooter : UIView
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _playerNameTMP;
    [SerializeField] private Image _playerIconImage;
    [SerializeField] private Button[] _playerInfoButtons;
    void OnEnable()
    {
        _playerNameTMP.text = PlayerInfoManager.Instance.PlayerName;
        PlayerInfoManager.Instance.OnChangedPlayerInfo += HandleUpdatedPlayerInfo;
        foreach (var button in _playerInfoButtons)
        {
            button.onClick.AddListener(HandleClickPlayerInfo);
        }
        UpdateUI();
    }
    void OnDisable()
    {
        PlayerInfoManager.Instance.OnChangedPlayerInfo -= HandleUpdatedPlayerInfo;
        foreach (var button in _playerInfoButtons)
        {
            button.onClick.RemoveListener(HandleClickPlayerInfo);
        }
    }
    private void UpdateUI()
    {
        _playerNameTMP.text = PlayerInfoManager.Instance.PlayerName;
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
}
