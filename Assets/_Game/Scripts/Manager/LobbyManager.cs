using System;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using System.Collections.Generic;

public class LobbyManager : SingletonMono<LobbyManager>
{
    public string RelayJoinCode { get; private set; }

    public event Action OnJoinedLobby;
    public event Action OnLeftLobby;
    public event Action<KickedFromLobbyEventArgs> OnKickedFromLobby;
    public event Action<UpdateCurrentLobbyEventArgs> OnUpdatedCurrentLobby;
    public event Action<ChangedLoobyListEventArgs> OnChangedLobbyList;

    public struct KickedFromLobbyEventArgs
    {
        public Lobby Lobby;
    }
    public struct UpdateCurrentLobbyEventArgs
    {
        public Lobby Lobby;
    }
    public struct ChangedLoobyListEventArgs
    {
        public List<Lobby> LobbyList;
    }

    private float _heartbeatTimer = 0f;
    private float _lobbyPollTimer = 0f;
    private float _refreshLobbyTimer = 5f;
    private Lobby _joinedLobby;
    private bool _hasStartedGame = false;

    void Start()
    {
        UIManager.Instance.ShowUI(EUIState.MainMenu);
    }
    void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer < 0)
            {
                float heartbeatTimerMax = 15f;
                _heartbeatTimer = heartbeatTimerMax;
                Debug.Log("Heartbeat");
                await LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
            }
        }
    }
    public bool IsLobbyHost()
    {
        return _joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
    private bool IsPlayerInLobby()
    {
        if (_joinedLobby == null || _joinedLobby.Players == null) return false;
        foreach (Player player in _joinedLobby.Players)
            if (player.Id == AuthenticationService.Instance.PlayerId) return true;
        return false;
    }
    private async void HandleLobbyPolling()
    {
        if (_joinedLobby == null)
            return;
        _lobbyPollTimer -= Time.deltaTime;
        if (_lobbyPollTimer < 0)
        {
            float lobbyPollTimerMax = 1.5f;
            _lobbyPollTimer = lobbyPollTimerMax;
            _joinedLobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);
            OnUpdatedCurrentLobby?.Invoke(new UpdateCurrentLobbyEventArgs(){ Lobby = _joinedLobby });

            if (!IsPlayerInLobby())
            {
                Debug.Log("Kicked from lobby");
                OnKickedFromLobby?.Invoke(new KickedFromLobbyEventArgs() { Lobby = _joinedLobby });
                _joinedLobby = null;
            }
        }
    }
    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
    }
    private void JoinLobby(string relayJoinCode)
    {
        Debug.Log("Join lobby " + relayJoinCode);
        if (string.IsNullOrEmpty(relayJoinCode))
        {
            Debug.Log("Invalid Relay code, wait");
            return;
        }
        RelayJoinCode = relayJoinCode;
    }

    public async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new()
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new(field: QueryFilter.FieldOptions.AvailableSlots, value: "0", op: QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new(asc: false, field: QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log("Query lobby list: " + queryResponse.Results.Count);
            OnChangedLobbyList?.Invoke(new ChangedLoobyListEventArgs() { LobbyList = queryResponse.Results });
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
}
