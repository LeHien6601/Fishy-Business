using System;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UILobbyItem : MonoBehaviour, IDeselectHandler
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _lobbyNameTMP;
    [SerializeField] private TextMeshProUGUI _lobbyStatusTMP;
    [SerializeField] private Button _btn;
    private string _relayJoinCode = "";
    private string _lobbyCode = "";
    public event Action<ClickedLobbyItemEventArgs> OnClickedLobbyItem;
    public event Action OnDeselectedLobbyItem;

    void OnEnable()
    {
        _btn.onClick.AddListener(HandleClick);
        
    }
    void OnDisable()
    {
        _btn.onClick.RemoveListener(HandleClick);
    }
    public struct ClickedLobbyItemEventArgs
    {
        public string LobbyCode;
        public string RelayJoinCode;
    }
    public void SetLobbyInfo(Lobby lobby)
    {
        _lobbyNameTMP.text = $"{lobby.Name}";
        _lobbyStatusTMP.text = $"{lobby.Players.Count}/{lobby.MaxPlayers} in Game";
        _relayJoinCode = lobby.Data[Constant.KEY_RELAY_JOIN_CODE].Value;
        _lobbyCode = lobby.Data[Constant.KEY_LOBBY_CODE].Value;
    }
    public void SetLobbyName(string name)
    {
        _lobbyNameTMP.text = name;
    }
    public void SetRelayJoinCode(string code)
    {
        _relayJoinCode = code;
    }
    private void HandleClick()
    {
        OnClickedLobbyItem?.Invoke(new ClickedLobbyItemEventArgs() { LobbyCode = _lobbyCode, RelayJoinCode = _relayJoinCode });
    }

    public void OnDeselect(BaseEventData eventData)
    {
        OnDeselectedLobbyItem?.Invoke();
    }
}
