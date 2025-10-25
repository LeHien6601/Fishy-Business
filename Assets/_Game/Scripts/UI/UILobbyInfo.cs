using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class UILobbyInfo : UIView
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _loobyNameText;
    [SerializeField] private Button _backButton;
    [SerializeField] private List<UILobbyMember> _uiMembers = new();
    void OnEnable()
    {
        UpdateUI();
        LobbyManager.Instance.OnUpdatedCurrentLobby += HandleUpdateLobby;
        _backButton.onClick.AddListener(HandleClickBack);
    }
    void OnDisable()
    {
        LobbyManager.Instance.OnUpdatedCurrentLobby -= HandleUpdateLobby;
        _backButton.onClick.RemoveListener(HandleClickBack);
    }
    private void HandleUpdateLobby(LobbyManager.UpdateCurrentLobbyEventArgs args)
    {
        UpdateUI();
    }
    private void UpdateUI()
    {
        if (LobbyManager.Instance.currentLobby != null)
        {
            Lobby lobby = LobbyManager.Instance.currentLobby;
            _loobyNameText.text = $"{lobby.Name}";
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
        }
    }
    private void HandleClickBack()
    {
        Hide();
        UIManager.Instance.ShowUI(EUIState.LobbyGameplay);
    }
}
