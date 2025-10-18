using TMPro;
using UnityEngine;

public class UIFooter : UIView
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _playerNameTMP;
    void OnEnable()
    {
        _playerNameTMP.text = GameManager.Instance.PlayerName;
        GameManager.Instance.OnUpdatedPlayerInfo += HandleUpdatedPlayerInfo;
    }
    void OnDisable()
    {
        GameManager.Instance.OnUpdatedPlayerInfo -= HandleUpdatedPlayerInfo;
    }
    private void HandleUpdatedPlayerInfo(GameManager.UpdatedPlayerInfoEventArgs args)
    {
        _playerNameTMP.text = args.PlayerName;
    }
}
