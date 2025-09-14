using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class UILobby : UIView
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _loobyNameText;
    [SerializeField] private TextMeshProUGUI _playerListText;
    [SerializeField] private Button _startGameButton;
    void OnEnable()
    {
        UpdateUI();
        LobbyManager.Instance.OnUpdatedCurrentLobby += HandleUpdateLobby;
        _startGameButton.onClick.AddListener(HandleClickStartGame);
    }
    void OnDisable()
    {
        LobbyManager.Instance.OnUpdatedCurrentLobby -= HandleUpdateLobby;
        _startGameButton.onClick.RemoveListener(HandleClickStartGame);
    }
    private void HandleUpdateLobby(LobbyManager.UpdateCurrentLobbyEventArgs args)
    {
        UpdateUI();
    }
    private void UpdateUI()
    {
        if (LobbyManager.Instance.currentLobby != null)
        {
            _startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
            Lobby lobby = LobbyManager.Instance.currentLobby;
            _loobyNameText.text = $"{lobby.Name} {lobby.Players.Count}/{lobby.MaxPlayers} {lobby.Data[Constant.KEY_RELAY_JOIN_CODE].Value}";
            _playerListText.text = "";
            foreach (var player in LobbyManager.Instance.currentLobby.Players)
            {
                _playerListText.text += player.Data[Constant.KEY_PLAYER_NAME].Value + "\n";
            }
        }
    }
    private void HandleClickStartGame()
    {
        GameManager.Instance.StartGame();
    }
}
