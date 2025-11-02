using System.Collections.Generic;
using HHDCore;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonMonoNet<GameManager>
{
    [SerializeField] private NetworkObject _playerPrefab;
    private Dictionary<ulong, PlayerNameDisplay> _spawnedPlayerNames = new();
    private Dictionary<ulong, string> _idMap = new(); //network id with auth id
    #region Cycle
    public void Start()
    {
        PlayerInfoManager.Instance.GenerateRandomPlayerInfo();
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }
    void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleLoadComplete;
        }
    }
    #endregion
    
    public void StartGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Starting Game...");
            NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleLoadComplete;
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
            _spawnedPlayerNames.Clear();
            Debug.Log("Game Started.");
        }
    }

    public void HandleLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log($"Client {clientId} finished loading scene {sceneName}");
        if (sceneName == "GameScene" && NetworkManager.Singleton.IsHost)
        {
            SpawnPlayerRpc(clientId);
            HandlePlayerJoinNetworkClientRpc(clientId);
        }
        else if (sceneName == "Lobby")
        {
            _spawnedPlayerNames.Clear();
        }
    }

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

    [ClientRpc]
    private void HandlePlayerJoinNetworkClientRpc(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        HandlePlayerJoinNetworkServerRpc(clientId, PlayerInfoManager.Instance.PlayerName, AuthenticationService.Instance.PlayerId);
    }
    [ServerRpc(RequireOwnership = false)]
    private void HandlePlayerJoinNetworkServerRpc(ulong clientId, string name, string authId)
    {
        _spawnedPlayerNames[clientId].SetPlayerName(name);
        _idMap[clientId] = authId;
    }
    public string GetAuthIdByNetId(ulong cliendId)
    {
        return _idMap.TryGetValue(cliendId, out var authId) ? authId : null;
    }

    [Rpc(SendTo.Server)]
    public void DespawnPlayerRpc(ulong clientId)
    {
        var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObject != null)
        {
            playerObject.Despawn(false);
            _spawnedPlayerNames.Remove(clientId);
            Debug.Log($"Despawned player for client {clientId}");
        }
    }
}
