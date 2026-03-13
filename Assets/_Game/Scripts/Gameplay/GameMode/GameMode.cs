using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class GameMode : ScriptableObject
{
    public float TurnInterval = 15;
    public abstract void Initialize(NetworkBoardManager manager, NetworkList<ulong> playerOrders);
    public abstract void StartGame(NetworkBoardManager manager);
    public abstract void HandlePlayerTurnStart(NetworkBoardManager manager, ulong playerId);
    public abstract void HandlePlayerActionComplete(NetworkBoardManager manager); // After play/discard
    public abstract bool CheckEndGameConditions(NetworkBoardManager manager, out bool isDogWin);
    public abstract void EndGame(NetworkBoardManager manager, bool isDogWin);

    // Optional hooks for phases (overridable)
    public virtual void StartPhase(NetworkBoardManager manager, GamePhase phase) { }
    public virtual void EndPhase(NetworkBoardManager manager, GamePhase phase) { }
    public virtual IEnumerator PhaseRoutine(NetworkBoardManager manager, GamePhase phase) { yield break; }

    // Helper to get random turn order
    protected List<ulong> GetRandomTurnOrder(NetworkList<ulong> playerOrders)
    {
        List<ulong> order = new();
        foreach (var playerId in playerOrders)
        {
            order.Add(playerId);
        }
        order.Shuffle(); // Implement Shuffle extension if needed
        return order;
    }
    public static string GetGamePhaseName(GamePhase gamePhase)
    {
        switch (gamePhase)
        {
            case GamePhase.Night:
                return "Night";
            case GamePhase.DayDiscussion:
                return "Discussion";
            case GamePhase.DayVoting:
                return "Voting";
            default:
            return "None";
        }
    }
}
[Serializable]
public struct GameData
{
    public int GameModeIndex;
    public float TurnInterval;
    public float VotingInterval;
    public float DayDiscussionInverval;
    public int MapIndex;
}

public enum GamePhase
{
    None,
    Night,
    DayDiscussion,
    DayVoting,
}

public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}