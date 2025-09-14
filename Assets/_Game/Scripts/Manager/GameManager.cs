using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonMonoNet<GameManager>
{
    [Header("Player Info")]
    public string PlayerName { get; private set; }
    public int PlayerIconID { get; private set; }
    [SerializeField] private NetworkObject _playerPrefab;

    private List<ulong> _spawnedPlayerIds = new();

    public void Start()
    {
        PlayerName = Utils.GetRandomPlayerName();
        PlayerIconID = Random.Range(0, 20); // Assuming there are 20 player icons
        Debug.Log($"Player Name: {PlayerName}, Icon ID: {PlayerIconID}");
    }

    async void OnEnable()
    {
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null)
        {
            await System.Threading.Tasks.Task.Yield();
        }
        NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleLoadComplete;
    }
    void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleLoadComplete;
        }
    }
    // Start game RPC
    public void StartGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

    private void HandleLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName == "GameScene")
        {
            SpawnPlayerRpc(clientId);
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

}
