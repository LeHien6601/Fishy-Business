using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using HHDCore;

public class LobbyManager : SingletonMono<LobbyManager>
{
    #region Properties
    public string RelayJoinCode { get; private set; }
    public Lobby currentLobby { get; private set; }
    public bool isHost { get; private set; }

    public event Action OnJoinedLobby;
    public event Action OnLeftLobby;
    public event Action<KickedFromLobbyEventArgs> OnKickedFromLobby;
    public event Action<UpdateCurrentLobbyEventArgs> OnUpdatedCurrentLobby;
    public event Action<UpdatedLoobyListEventArgs> OnUpdatedLobbyList;

    private Coroutine _heartbeatCoroutine;
    private Coroutine _pollLobbyCoroutine;
    #endregion

    #region Event Args
    public struct KickedFromLobbyEventArgs
    {
        public Lobby Lobby;
    }
    public struct UpdateCurrentLobbyEventArgs
    {
        public Lobby Lobby;
    }
    public struct UpdatedLoobyListEventArgs
    {
        public List<Lobby> LobbyList;
    }
    #endregion

    #region Cycle
    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Debug.Log("Authenticated: " + AuthenticationService.Instance.PlayerId);
            UIManager.Instance.ShowUI(EUIState.MainMenu);
        }
        catch (ServicesInitializationException e)
        {
            Debug.LogException(e);
        }
    }
    private void OnEnable()
    {
        OnKickedFromLobby += HandleKickedFromLobby;
    }
    private void OnDisable()
    {
        if (currentLobby != null && isHost && _heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
        }
        OnKickedFromLobby -= HandleKickedFromLobby;
    }
    #endregion

    #region Lobby Operations
    public async Task CreateLobbyAsync(string lobbyName)
    {
        try
        {
            var createOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayerData(GameManager.Instance.PlayerName, GameManager.Instance.PlayerIconID),
                Data = new Dictionary<string, DataObject>
                {
                    {Constant.KEY_HOST_ID, new DataObject(DataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId)},
                    {Constant.KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Public, "")}
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, Constant.MAX_PLAYERS, createOptions);
            isHost = true;
            Debug.Log($"Lobby created: {currentLobby.Id}, Code: {currentLobby.LobbyCode}");

            // Start Relay and heartbeats
            currentLobby = await RelayManager.Instance.SetupRelay(currentLobby);
            Debug.Log("Relay Join Code: " + currentLobby.Data[Constant.KEY_RELAY_JOIN_CODE].Value);
            Debug.Log("Lobby Code: " + currentLobby.LobbyCode);
            _heartbeatCoroutine = StartCoroutine(HeartbeatLobby(currentLobby.Id));
            _pollLobbyCoroutine = StartCoroutine(PollLobbyCoroutine());
            OnJoinedLobby?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public async Task<List<Lobby>> QueryLobbiesAsync()
    {
        try
        {
            var queryOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new (QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                },
                Order = new List<QueryOrder> { new(false, QueryOrder.FieldOptions.Created) },
                Count = 10
            };

            var response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
            OnUpdatedLobbyList?.Invoke(new UpdatedLoobyListEventArgs() { LobbyList = response.Results });
            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
            return new List<Lobby>();
        }
    }
    public async Task JoinLobbyByCodeAsync(string lobbyCode, string playerName = "Player", int iconId = 0)
    {
        try
        {
            var joinOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayerData(playerName, iconId)
            };
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            isHost = false;
            Debug.Log($"Joined lobby: {currentLobby.Id}");

            await RelayManager.Instance.JoinRelay(currentLobby);
            _heartbeatCoroutine = StartCoroutine(HeartbeatLobby(currentLobby.Id));
            _pollLobbyCoroutine = StartCoroutine(PollLobbyCoroutine());
            OnJoinedLobby?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public async Task QuickJoinAsync(string playerName = "Player", int iconId = 0)
    {
        try
        {
            var joinOptions = new QuickJoinLobbyOptions
            {
                Player = GetPlayerData(playerName, iconId)
            };
            currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(joinOptions);
            isHost = false;
            Debug.Log($"Quick joined lobby: {currentLobby.Id}");

            await RelayManager.Instance.JoinRelay(currentLobby);
            _heartbeatCoroutine = StartCoroutine(HeartbeatLobby(currentLobby.Id));
            _pollLobbyCoroutine = StartCoroutine(PollLobbyCoroutine());
            OnJoinedLobby?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
            throw;
        }
    }

    public async Task UpdatePlayerDataAsync(string playerName, int iconId)
    {
        try
        {
            var updateOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    {Constant.KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)},
                    {Constant.KEY_PLAYER_ICON_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, iconId.ToString())}
                }
            };
            await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, updateOptions);
            Debug.Log("Player data updated");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public async Task LeaveLobbyAsync()
    {
        try
        {
            if (currentLobby != null)
            {
                if (isHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }
                currentLobby = null;
                isHost = false;
                if (_heartbeatCoroutine != null)
                {
                    StopCoroutine(_heartbeatCoroutine);
                    _heartbeatCoroutine = null;
                }
                if (_pollLobbyCoroutine != null)
                {
                    StopCoroutine(_pollLobbyCoroutine);
                    _pollLobbyCoroutine = null;
                }
                Debug.Log("Left lobby");
                OnLeftLobby?.Invoke();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public async void KickPlayerAsync(string playerId)
    {
        try
        {
            if (!isHost)
            {
                Debug.LogWarning("Only the host can kick players.");
                return;
            }
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            Debug.Log($"Kicked player: {playerId}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }
    private Player GetPlayerData(string playerName, int iconId)
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {Constant.KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)},
                {Constant.KEY_PLAYER_ICON_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, iconId.ToString())}
            }
        };
    }


    private IEnumerator HeartbeatLobby(string lobbyId)
    {
        while (true)
        {
            yield return Utils.GetWaitForSeconds(15f);
            try
            {
                LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
    }
    private IEnumerator PollLobbyCoroutine()
    {
        var wait = Utils.GetWaitForSeconds(5f);
        while (true)
        {
            yield return wait;
            _ = PollLobbyAsync();
        }
    }
    public async Task PollLobbyAsync()
    {
        if (currentLobby == null) return;
        try
        {
            var updatedLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            if (updatedLobby != null)
            {
                // Check if the player has been removed from the lobby
                bool isPlayerInLobby = updatedLobby.Players.Exists(p => p.Id == AuthenticationService.Instance.PlayerId);
                if (!isPlayerInLobby)
                {
                    Debug.Log("You have been removed from the lobby.");
                    currentLobby = null;
                    isHost = false;
                    if (_heartbeatCoroutine != null)
                    {
                        StopCoroutine(_heartbeatCoroutine);
                        _heartbeatCoroutine = null;
                    }
                    if (_pollLobbyCoroutine != null)
                    {
                        StopCoroutine(_pollLobbyCoroutine);
                        _pollLobbyCoroutine = null;
                    }
                    OnKickedFromLobby?.Invoke(new KickedFromLobbyEventArgs() { Lobby = updatedLobby });
                    return;
                }

                currentLobby = updatedLobby;
                OnUpdatedCurrentLobby?.Invoke(new UpdateCurrentLobbyEventArgs() { Lobby = updatedLobby });
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }
    #endregion

    #region Event Handlers
    private void HandleKickedFromLobby(KickedFromLobbyEventArgs args)
    {
        GameManager.Instance.DespawnPlayerRpc(NetworkManager.Singleton.LocalClientId);
        NetworkManager.Singleton.Shutdown();
        NetworkManager.Singleton.SceneManager.OnLoadComplete += GameManager.Instance.HandleLoadComplete;
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        GameManager.Instance.HandleLoadComplete(NetworkManager.Singleton.LocalClientId, "Lobby", LoadSceneMode.Single);
        EventSystem.current.SetSelectedGameObject(null);
        UIManager.Instance.ShowUI(EUIState.MainMenu);
    }
    #endregion
}