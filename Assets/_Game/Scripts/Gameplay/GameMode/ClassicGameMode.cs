using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "ClassicGameMode", menuName = "GameModes/Classic")]
public class ClassicGameMode : GameMode
{
    public override void Initialize(NetworkBoardManager manager, NetworkList<ulong> playerOrders)
    {
        // Classic: Fixed order
        List<ulong> order = new();
        foreach (var playerId in playerOrders)
        {
            order.Add(playerId);
        }
        manager.SetTurnOrder(order);
    }

    public override void StartGame(NetworkBoardManager manager)
    {
        // Classic: No phases, just start turns
        manager.StartNextTurn();
    }

    public override void HandlePlayerTurnStart(NetworkBoardManager manager, ulong playerId)
    {
        // Classic: Standard turn start
        manager.RequestNextTurn(playerId);
    }

    public override void HandlePlayerActionComplete(NetworkBoardManager manager)
    {
        // Classic: Draw card, then next turn
        manager.RequestEndCurrentTurn();
    }

    public override bool CheckEndGameConditions(NetworkBoardManager manager, out bool isDogWin)
    {
        isDogWin = true;
        // Classic: Check for out of cards or goal reached (moved from NetworkBoardManager)
        if (manager.CheckForOutOfCards())
        {
            isDogWin = false;
            return true;
        }
        // Goal check happens in ConfirmCardPlacementClientRpc callback
        return false;
    }

    public override void EndGame(NetworkBoardManager manager, bool isDogWin)
    {
        // Classic end game logic (moved from NetworkBoardManager)
        manager.BroadcastGameEnd(isDogWin);
    }
}