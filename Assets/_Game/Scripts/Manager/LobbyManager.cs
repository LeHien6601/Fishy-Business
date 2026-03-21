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
    private bool _initServices = false;
    public bool InitServices => _initServices;

    public event Action OnJoinedLobby;
    public event Action OnLeftLobby;
    public event Action<KickedFromLobbyEventArgs> OnKickedFromLobby;
    public event Action<UpdateCurrentLobbyEventArgs> OnUpdatedCurrentLobby;
    public event Action<UpdatedLoobyListEventArgs> OnUpdatedLobbyList;
    public event Action<Player> OnPlayerJoinedLobby;
    public event Action<PlayerLeftLobbyEventArgs> OnPlayerLeftLobby;
    private Coroutine _heartbeatCoroutine;
    private Coroutine _pollLobbyCoroutine;
    private HashSet<string> _currentPlayerIds = new();
    private Dictionary<string, string> _playerIdMapToName = new();
    #endregion

    #region Structs
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
    public struct PlayerLeftLobbyEventArgs
    {
        public string AuthId;
        public string Name;
    }
    [Serializable]
    public struct PlayerInfo : INetworkSerializable
    {
        public string Name;
        public int IconId;
        public bool Found;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref IconId);
            serializer.SerializeValue(ref Found);
        }
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
            _initServices = true;
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
        OnUpdatedCurrentLobby += HandleUpdatedLobby;
        PlayerInfoManager.Instance.OnChangedPlayerInfo += HandleUpdatePlayerInfo;
        GameplayManager.Instance.OnStartGame += HandleStartGame;
        GameplayManager.Instance.OnEndGame += HandleEndGame;
        OnPlayerJoinedLobby += HandlePlayerJoinLobby;
    }
    private void OnDisable()
    {
        if (currentLobby != null && isHost && _heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
        }
        OnKickedFromLobby -= HandleKickedFromLobby;
        OnUpdatedCurrentLobby += HandleUpdatedLobby;
        PlayerInfoManager.Instance.OnChangedPlayerInfo -= HandleUpdatePlayerInfo;
        GameplayManager.Instance.OnStartGame -= HandleStartGame;
        GameplayManager.Instance.OnEndGame -= HandleEndGame;
        OnPlayerJoinedLobby -= HandlePlayerJoinLobby;
    }
    #endregion

    #region Lobby Operations
    public async Task CreateLobbyAsync(string lobbyName, int mapIndex)
    {
        try
        {
            var createOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayerData(
                    PlayerInfoManager.Instance.PlayerName,
                    PlayerInfoManager.Instance.PlayerIconId
                ),
                Data = new Dictionary<string, DataObject>
                {
                    {Constant.KEY_HOST_ID, new DataObject(DataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId)},
                    {Constant.KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Public, "")},
                    {Constant.KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Public, "false", DataObject.IndexOptions.S1)},
                    {Constant.KEY_GAME_MODE_DATA, new DataObject(DataObject.VisibilityOptions.Member, Utils.GetJsonGameModeData(0,mapIndex))}
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, Constant.MAX_PLAYERS, createOptions);
            isHost = true;
            _currentPlayerIds.Clear();
            _currentPlayerIds.Add(AuthenticationService.Instance.PlayerId);
            _playerIdMapToName.Clear();
            _playerIdMapToName[AuthenticationService.Instance.PlayerId] = PlayerInfoManager.Instance.PlayerName;
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
    public async Task<bool> UpdateLobbyNameAsync(string newName)
    {
        try
        {
            var updateOptions = new UpdateLobbyOptions
            {
                Name = newName
            };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, updateOptions);
            Debug.Log("Lobby name updated");
            OnUpdatedCurrentLobby?.Invoke(new UpdateCurrentLobbyEventArgs() { Lobby = currentLobby });
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    public async Task<bool> UpdateLobbyGameDataAsync(string json)
    {
        try
        {
            Dictionary<string, DataObject> updatedData = currentLobby.Data;
            updatedData[Constant.KEY_GAME_MODE_DATA] = new DataObject(DataObject.VisibilityOptions.Member, json);
            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
            {
                Data = updatedData
            });
            Debug.Log("Lobby game data updated");
            OnUpdatedCurrentLobby?.Invoke(new UpdateCurrentLobbyEventArgs() { Lobby = currentLobby });
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
            return false;
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
                    new (QueryFilter.FieldOptions.S1, "false", QueryFilter.OpOptions.EQ)
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

    public async Task QuickJoinAsync(string playerName, int iconId)
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
            OnUpdatedCurrentLobby?.Invoke(new UpdateCurrentLobbyEventArgs() { Lobby = currentLobby });
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
                {Constant.KEY_PLAYER_ICON_ID, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, iconId.ToString())},
            }
        };
    }
    public int GetPlayerIndex(string authId)
    {
        int index = 0;
        foreach (var player in currentLobby.Players)
        {
            if (player.Id == authId) return index;
            index++;
        }
        return index;
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
            Lobby updatedLobby;
            try
            {
                updatedLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                // Lobby was deleted (probably by host leaving)
                Debug.Log("Lobby no longer exists - probably deleted by host");
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
                OnKickedFromLobby?.Invoke(new KickedFromLobbyEventArgs { Lobby = currentLobby });
                return;
            }
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

    async void OnApplicationQuit()
    {
        if (currentLobby == null) return;
        await LeaveLobbyAsync();
    }
    #endregion

    #region Event Handlers
    private void HandleKickedFromLobby(KickedFromLobbyEventArgs args)
    {
        GameManager.Instance.DespawnPlayerRpc(NetworkManager.Singleton.LocalClientId);
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        GameManager.Instance.HandleLoadComplete(NetworkManager.Singleton.LocalClientId, "Lobby", LoadSceneMode.Single);
        EventSystem.current.SetSelectedGameObject(null);
        UIManager.Instance.ShowUI(EUIState.MainMenu);
        UIManager.Instance.HideUI(EUIState.LobbyGameplay);
        Cursor.lockState = CursorLockMode.None;
    }
    private async void HandleUpdatePlayerInfo(PlayerInfoManager.ChangedPlayerInfoEventArgs args)
    {
        if (currentLobby == null) return;
        await UpdatePlayerDataAsync(args.NewPlayerName, args.NewPlayerIconId);
    }

    private void HandleUpdatedLobby(UpdateCurrentLobbyEventArgs args)
    {
        if (!isHost) return;
        var previousPlayerIds = new HashSet<string>(_currentPlayerIds);
        var currentPlayers = new HashSet<string>();
        foreach (var player in args.Lobby.Players)
        {
            currentPlayers.Add(player.Id);
            if (_currentPlayerIds.Contains(player.Id)) continue;
            OnPlayerJoinedLobby?.Invoke(player);
            Debug.Log($"New player joined: {player.Id}");
        }
        foreach (var oldId in previousPlayerIds)
        {
            if (currentPlayers.Contains(oldId)) continue;
            OnPlayerLeftLobby?.Invoke(new()
            {
                AuthId = oldId,
                Name = _playerIdMapToName.TryGetValue(oldId, out string playerName) ? playerName : "Anonymous"
            });
            Debug.Log($"Player left lobby: {oldId} " + (_playerIdMapToName.TryGetValue(oldId, out string displayName) ? displayName : "Anonymous"));
        }
        _currentPlayerIds.Clear();
        _playerIdMapToName.Clear();
        foreach (var player in args.Lobby.Players)
        {
            _currentPlayerIds.Add(player.Id);
            _playerIdMapToName[player.Id] = player.Data[Constant.KEY_PLAYER_NAME].Value;
        }
    }
    private async void HandlePlayerJoinLobby(Player player)
    {
        if (!isHost) return;
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            await Task.Yield();
            if (currentLobby.Data[Constant.KEY_START_GAME].Value == "true")
            {
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, player.Id);
                return;
            }
        }
    }
    private async void HandleStartGame(ulong cliendId)
    {
        if (!isHost) return;
        Dictionary<string, DataObject> updatedData = currentLobby.Data;
        updatedData[Constant.KEY_START_GAME] = new DataObject(DataObject.VisibilityOptions.Public, "true", DataObject.IndexOptions.S1);
        currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
        {
            Data = updatedData
        });
    }
    private async void HandleEndGame(ulong cliendId)
    {
        if (!isHost) return;
        Dictionary<string, DataObject> updatedData = currentLobby.Data;
        updatedData[Constant.KEY_START_GAME] = new DataObject(DataObject.VisibilityOptions.Public, "false", DataObject.IndexOptions.S1);
        currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
        {
            Data = updatedData
        });
    }
    #endregion

    #region Getters
    public PlayerInfo GetPlayerInfoFromPlayerId(string authId)
    {
        if (currentLobby == null)
        {
            return new PlayerInfo { Found = false };
        }
        foreach (var player in currentLobby.Players)
        {
            if (player.Id == authId)
            {
                string name = player.Data[Constant.KEY_PLAYER_NAME].Value;
                int iconId = int.Parse(player.Data[Constant.KEY_PLAYER_ICON_ID].Value);
                
                return new PlayerInfo
                {
                    Name = name,
                    IconId = iconId,
                    Found = true
                };
            }
        }

        return new PlayerInfo { Found = false };
    }
    #endregion
}