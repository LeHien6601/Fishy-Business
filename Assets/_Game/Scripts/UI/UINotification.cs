using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class UINotification : UIView
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _lobbyInGameNotiTMP;
    private bool _isShowingNoti = false;

    void OnEnable()
    {
        _lobbyInGameNotiTMP.rectTransform.localScale = Vector3.zero;
        LobbyManager.Instance.OnPlayerJoinedLobby += HandlePlayerJoinLobby;
        LobbyManager.Instance.OnPlayerLeftLobby += HandlePlayerLeaveLobby;
    }
    void OnDisable()
    {
        LobbyManager.Instance.OnPlayerJoinedLobby -= HandlePlayerJoinLobby;
        LobbyManager.Instance.OnPlayerLeftLobby -= HandlePlayerLeaveLobby;
    }

    private void HandlePlayerJoinLobby(Player player)
    {
        ShowNoti($"{player.Data[Constant.KEY_PLAYER_NAME].Value} joins lobby!");
    }
    private void HandlePlayerLeaveLobby(LobbyManager.PlayerLeftLobbyEventArgs args)
    {
        ShowNoti($"{args.Name} left lobby!");
    }
    private async void ShowNoti(string noti)
    {
        while (_isShowingNoti)
            await Task.Yield();
        _isShowingNoti = true;
        _lobbyInGameNotiTMP.text = noti;
        _lobbyInGameNotiTMP.rectTransform.localScale = Vector3.zero;
        _lobbyInGameNotiTMP.rectTransform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        await Task.Delay(3000);
        _lobbyInGameNotiTMP.rectTransform.DOScale(0f, 0.5f).SetEase(Ease.InBack);
        await Task.Delay(1000);
        _isShowingNoti = false;
    }
}