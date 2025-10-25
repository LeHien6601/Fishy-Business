using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameplayManager : NetworkBehaviour
{
    public static GameplayManager Instance;

    [SerializeField] private BoardManager boardPrefab;
    private Dictionary<ulong, BoardManager> playerBoards = new();
    private List<int> sharedDeck = new(); // deck chung giữa tất cả players
    private NetworkVariable<int> currentPlayerTurn = new(writePerm: NetworkVariableWritePermission.Server);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GenerateSharedDeck();
        }
    }

    private void GenerateSharedDeck()
    {
        sharedDeck.Clear();
        List<int> cardPool = new List<int>();
        for (int i = 0; i < 40; i++)
            cardPool.Add(i);

        // Fisher–Yates shuffle
        for (int i = 0; i < cardPool.Count; i++)
        {
            int rand = Random.Range(i, cardPool.Count);
            (cardPool[i], cardPool[rand]) = (cardPool[rand], cardPool[i]);
        }
        sharedDeck.AddRange(cardPool);
    }

    public void RegisterBoard(ulong clientId, BoardManager board)
    {
        playerBoards[clientId] = board;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Player {clientId} requested start game.");

        foreach (var kvp in playerBoards)
        {
            StartGameClientRpc(sharedDeck.ToArray(), kvp.Key);
        }

        currentPlayerTurn.Value = 0;
    }

    [ClientRpc]
    private void StartGameClientRpc(int[] sharedDeckArray, ulong targetClient)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClient) return;
        Debug.Log($"Starting game for player {targetClient}");

        var bm = FindAnyObjectByType<BoardManager>();
        bm.InitializeFromDeck(sharedDeckArray);
        bm.StartGame();
    }

    // Khi player đánh card
    [ServerRpc(RequireOwnership = false)]
    public void PlayCardServerRpc(int cardId, Vector2Int slot, bool flipped, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (senderId != (ulong)currentPlayerTurn.Value)
        {
            Debug.LogWarning($"Player {senderId} tried to play out of turn!");
            return;
        }

        // Gửi xuống toàn bộ client để render hành động
        PlayCardClientRpc(cardId, slot, flipped);
        NextTurn();
    }

    [ClientRpc]
    private void PlayCardClientRpc(int cardId, Vector2Int slot, bool flipped)
    {
        var bm = FindAnyObjectByType<BoardManager>();
        bm.ApplyPlayCard(cardId, slot, flipped);
    }

    private void NextTurn()
    {
        currentPlayerTurn.Value = (currentPlayerTurn.Value + 1) % NetworkManager.Singleton.ConnectedClients.Count;
    }

    public bool IsMyTurn()
    {
        return (int)NetworkManager.Singleton.LocalClientId == currentPlayerTurn.Value;
    }
}
