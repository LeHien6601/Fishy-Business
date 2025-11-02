using System.Collections.Generic;
using System.Threading.Tasks;
using HHDCore;
using UnityEngine;

public class UIEndGame : UIView
{
    #region Properties 
    [Header("References")]
    [SerializeField] private CustomLayout _layout;
    [SerializeField] private List<UIPlayerEndGame> _uiPlayers = new();
    #endregion

    #region View behavior
    public override async void Show()
    {
        UpdateData(GameplayManager.Instance.GetWinnerInfos());
        base.Show();
        await Task.Delay(5000);
        Hide();
    }
    #endregion

    #region Setter
    public void UpdateData(List<LobbyManager.PlayerInfo> playerInfos)
    {
        for (int i = 0; i < _uiPlayers.Count; i++)
        {
            if (i < playerInfos.Count)
            {
                _uiPlayers[i].UpdatePlayerInfo(playerInfos[i].Name, playerInfos[i].IconId);
                _uiPlayers[i].gameObject.SetActive(true);
            }
            else
            {
                _uiPlayers[i].gameObject.SetActive(false);
            }
        }
        _layout.UpdateLayoutFitType();
    }
    #endregion
}
