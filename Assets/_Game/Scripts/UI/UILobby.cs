using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.XR;

public class UILobby : UIView
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _loobyNameText;
    [SerializeField] private TextMeshProUGUI _playerListText;
    void OnEnable()
    {
        UpdateUI();
        LobbyManager.Instance.OnUpdatedCurrentLobby += HandleUpdateLobby;
    }
    void OnDisable()
    {
        LobbyManager.Instance.OnUpdatedCurrentLobby -= HandleUpdateLobby;
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
            _loobyNameText.text = $"{lobby.Name} {lobby.Players.Count}/{lobby.MaxPlayers} {lobby.Data[Constant.KEY_RELAY_JOIN_CODE].Value}";
            _playerListText.text = "";
            foreach (var player in LobbyManager.Instance.currentLobby.Players)
            {
                _playerListText.text += player.Data[Constant.KEY_PLAYER_NAME].Value + "\n";
            }
        }
    }
}
