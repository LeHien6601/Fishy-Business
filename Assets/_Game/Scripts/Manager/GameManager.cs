using System.Collections.Generic;
using HHDCore;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonMonoNet<GameManager>
{
    #region Properties
    [SerializeField] private NetworkObject _playerPrefab;
    private Dictionary<ulong, PlayerNameDisplay> _spawnedPlayerNames = new();
    private Dictionary<ulong, string> _idMap = new(); //network id with auth id
    private EGameState _currentGameState = EGameState.MainMenu;
    public EGameState CurrentGameState => _currentGameState;
    #endregion

    #region Cycle
    public void Start()
    {
        PlayerInfoManager.Instance.GenerateRandomPlayerInfo();
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        LobbyManager.Instance.OnUpdatedCurrentLobby += HandleUpdateLobbyData;
    }
    void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleLoadComplete;
        }
        LobbyManager.Instance.OnUpdatedCurrentLobby -= HandleUpdateLobbyData;
    }
    #endregion
    
    /// <summary>
    /// Host-only: Starts the game by loading the GameScene for all clients.
    /// Registers a callback for when clients finish loading.
    /// </summary>
    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        Debug.Log("Starting Game...");
        NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleLoadComplete;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        _spawnedPlayerNames.Clear();
        _idMap.Clear();
        Debug.Log("Game Started.");
    }
    #region Handlers
    /// <summary>
    /// Called by Netcode whenever ANY client finishes loading a scene.
    /// We use this to spawn the player object exactly when the client is ready.
    /// </summary>
    public void HandleLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log($"Client {clientId} finished loading scene {sceneName}");
        if (sceneName == "GameScene")
        {
            SpawnPlayerRpc(clientId);
            HandlePlayerJoinNetworkClientRpc(clientId);
            UIManager.Instance.ShowUI(EUIState.Notification);
            UIManager.Instance.ShowUI(EUIState.TextChat);
        }
        // Leave lobby or kicked from lobby -> clean up
        else if (sceneName == "Lobby")
        {
            _spawnedPlayerNames.Clear();
            _currentGameState = EGameState.MainMenu;
            HandlePlayerLeaveNetworkClientRpc(clientId);
            UIManager.Instance.HideUI(EUIState.InGame);
            UIManager.Instance.HideUI(EUIState.TextChat);
            UIManager.Instance.HideUI(EUIState.Notification);
        }
    }
    /// <summary>
    /// ClientRpc → Sent to the client that just finished loading.
    /// That client then reports back its name and Auth ID to the host.
    /// </summary>
    [ClientRpc]
    private void HandlePlayerJoinNetworkClientRpc(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        HandlePlayerJoinNetworkServerRpc(clientId, PlayerInfoManager.Instance.PlayerName, AuthenticationService.Instance.PlayerId);
        _currentGameState = EGameState.InGame;
        SoundManager.PlayMusic(SoundType.Lobby);
        UIManager.Instance.ShowUI(EUIState.Notification);
        UIManager.Instance.ShowUI(EUIState.TextChat);
    }
    [ClientRpc]
    private void HandlePlayerLeaveNetworkClientRpc(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        _currentGameState = EGameState.MainMenu;
        UIManager.Instance.HideUI(EUIState.InGame);
        UIManager.Instance.HideUI(EUIState.TextChat);
        UIManager.Instance.HideUI(EUIState.Notification);
    }

    /// <summary>
    /// ServerRpc → Called by the client to register its name and Auth ID on the host.
    /// Host stores the mapping and updates the floating name UI.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void HandlePlayerJoinNetworkServerRpc(ulong clientId, string name, string authId)
    {
        _idMap[clientId] = authId;
        _spawnedPlayerNames[clientId].SetPlayerName(name, authId);
    }
    /// <summary>
    /// Triggered whenever the lobby data changes (e.g., someone edits their name).
    /// Updates the in-game name display for all players who are already in the GameScene.
    /// </summary>
    private void HandleUpdateLobbyData(LobbyManager.UpdateCurrentLobbyEventArgs args)
    {
        foreach (var player in args.Lobby.Players)
        {
            if (GetNetIdByAuthId(player.Id, out ulong netId))
                HandleUpdatePlayerDataServerRpc(netId, player.Data[Constant.KEY_PLAYER_NAME].Value);
        }
    }
    /// <summary>
    /// ServerRpc used by HandleUpdateLobbyData to actually change the name display.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void HandleUpdatePlayerDataServerRpc(ulong clientId, string name)
    {
        _spawnedPlayerNames[clientId].SetPlayerName(name, GetAuthIdByNetId(clientId));
    }
    #endregion

    #region Getter
    /// <summary>
    /// Retrieve the UGS Authentication ID for a given Netcode client ID.
    /// </summary>
    public string GetAuthIdByNetId(ulong clientId)
    {
        return _idMap.TryGetValue(clientId, out var authId) ? authId : null;
    }
    public int GetPlayerIndexByClientId(ulong clientId)
    {
        int i = 0;
        foreach (var player in _idMap)
        {
            if (player.Key == clientId) return i++;
        }
        return 0;
    }
    /// <summary>
    /// Find the Netcode client ID that belongs to a specific UGS Authentication ID.
    /// Used heavily for syncing lobby data → in-game objects.
    /// </summary>
    public bool GetNetIdByAuthId(string authId, out ulong clientId)
    {
        foreach (var pair in _idMap)
        {
            if (pair.Value == authId)
            {
                clientId = pair.Key;
                return true;
            }
        }
        clientId = 0;
        return false;
    }
    #endregion

    #region Spawn/Despawn player
    /// <summary>
    /// Server-only RPC that instantiates and spawns a player object for the given client.
    /// Called from HandleLoadComplete when a client finishes loading GameScene.
    /// </summary>
    [Rpc(SendTo.Server)]
    private void SpawnPlayerRpc(ulong clientId)
    {
        if (_spawnedPlayerNames.ContainsKey(clientId))
        {
            Debug.LogWarning($"Player for client {clientId} has already been spawned.");
            return;
        }
        var playerInstance = Instantiate(_playerPrefab);
        playerInstance.SpawnAsPlayerObject(clientId, true);
        PlayerNameDisplay nameDisplay = playerInstance.GetComponentInChildren<PlayerNameDisplay>();
        _spawnedPlayerNames[clientId] = nameDisplay;
        nameDisplay.SetPlayerName("Anonymous", GetAuthIdByNetId(clientId));
        Debug.Log($"Spawned player for client {clientId}");
    }
    /// <summary>
    /// Server-only RPC to despawn a player's NetworkObject (when kick player or player left lobby).
    /// </summary>
    [Rpc(SendTo.Server)]
    public void DespawnPlayerRpc(ulong clientId)
    {
        var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObject != null && playerObject.IsSpawned)
        {
            playerObject.Despawn(false);
            _spawnedPlayerNames.Remove(clientId);
            _idMap.Remove(clientId);
            Debug.Log($"Despawned player for client {clientId}");
        }
    }
#endregion
public enum EGameState
    {
        MainMenu,
        InGame
    }
}
