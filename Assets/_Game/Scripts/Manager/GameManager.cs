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
        if (sceneName == "GameScene" && NetworkManager.Singleton.IsHost)
        {
            SpawnPlayerRpc(clientId);
            HandlePlayerJoinNetworkClientRpc(clientId);
        }
        // Leave lobby or kicked from lobby -> clean up
        else if (sceneName == "Lobby")
        {
            _spawnedPlayerNames.Clear();
            UIManager.Instance.HideUI(EUIState.InGame);
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
    }

    /// <summary>
    /// ServerRpc → Called by the client to register its name and Auth ID on the host.
    /// Host stores the mapping and updates the floating name UI.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void HandlePlayerJoinNetworkServerRpc(ulong clientId, string name, string authId)
    {
        _spawnedPlayerNames[clientId].SetPlayerName(name);
        _idMap[clientId] = authId;
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
    [ServerRpc(RequireOwnership = false)]
    private void HandleUpdatePlayerDataServerRpc(ulong clientId, string name)
    {
        _spawnedPlayerNames[clientId].SetPlayerName(name);
    }
    #endregion

    #region Getter
    /// <summary>
    /// Retrieve the UGS Authentication ID for a given Netcode client ID.
    /// </summary>
    public string GetAuthIdByNetId(ulong cliendId)
    {
        return _idMap.TryGetValue(cliendId, out var authId) ? authId : null;
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
        nameDisplay.SetPlayerName("Anonymous");
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
            Debug.Log($"Despawned player for client {clientId}");
        }
    }
#endregion
}
