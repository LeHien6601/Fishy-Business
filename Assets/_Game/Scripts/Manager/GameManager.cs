using System;
using System.Collections.Generic;
using HHDCore;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonMonoNet<GameManager>
{
    [SerializeField] private NetworkObject _playerPrefab;
    private List<ulong> _spawnedPlayerIds = new();

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
    public void StartGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Starting Game...");
            NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleLoadComplete;
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
            _spawnedPlayerIds.Clear();
            Debug.Log("Game Started.");
        }
    }

    public void HandleLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log($"Client {clientId} finished loading scene {sceneName}");
        if (sceneName == "GameScene" && NetworkManager.Singleton.IsServer)
        {
            SpawnPlayerRpc(clientId);
        }
        else if (sceneName == "Lobby")
        {
            _spawnedPlayerIds.Clear();
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayerRpc(ulong clientId)
    {
        if (_spawnedPlayerIds.Contains(clientId))
        {
            Debug.LogWarning($"Player for client {clientId} has already been spawned.");
            return;
        }
        _spawnedPlayerIds.Add(clientId);
        var playerInstance = Instantiate(_playerPrefab);
        playerInstance.SpawnAsPlayerObject(clientId, true);
        Debug.Log($"Spawned player for client {clientId}");
    }

    [Rpc(SendTo.Server)]
    public void DespawnPlayerRpc(ulong clientId)
    {
        var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObject != null)
        {
            playerObject.Despawn(false);
            _spawnedPlayerIds.Remove(clientId);
            Debug.Log($"Despawned player for client {clientId}");
        }
    }
}
