using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using HHDCore;
using TMPro;
using UnityEngine;

public class UIEndGame : UIView
{
    #region Properties 
    [Header("References")]
    [SerializeField] private CustomLayout _layout;
    [SerializeField] private List<UIPlayerEndGame> _uiPlayers = new();
    [SerializeField] private TextMeshProUGUI _loseTMP;
    #endregion

    #region View behavior
    public override async void Show()
    {
        UpdateData(GameplayManager.Instance.GetWinnerInfos());
        base.Show();
        await Task.Delay((int)(1000 * Constant.RESTART_INTERVAL));
        Hide();
        UIManager.Instance.HideUI(EUIState.InGame);
    }
    #endregion

    #region Setter
    public async void UpdateData(List<LobbyManager.PlayerInfo> playerInfos)
    {
        _loseTMP.gameObject.SetActive(false);
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
        if (playerInfos.Count == 0)
        {
            SoundManager.Play2D(SoundType.Lose);
            _loseTMP.gameObject.SetActive(true);
            _loseTMP.rectTransform.localScale = Vector3.zero;
            _loseTMP.rectTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            _loseTMP.rectTransform.DOShakePosition(2f, 10f, 20, 90f, false, true);
            await Task.Delay(2500);
            _loseTMP.rectTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
        }
        else
        {
            SoundManager.Play2D(SoundType.Victory);
        }
    }
    #endregion
}
