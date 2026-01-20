using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "PhasedGameMode", menuName = "GameModes/Phased")]
public class PhasedGameMode : GameMode
{
    [SerializeField] private float _dayDiscussionTime = 10f;

    private List<ulong> _currentNightOrder = new();
    private int _currentTurnIndex;

    public override void StartPhase(NetworkBoardManager manager, GamePhase phase)
    {
        base.StartPhase(manager, phase);
        Debug.Log("Start phase " + phase.ToString());
    }
    public override void EndPhase(NetworkBoardManager manager, GamePhase phase)
    {
        base.EndPhase(manager, phase);
        Debug.Log("End phase " + phase.ToString());
    }

    public override void Initialize(NetworkBoardManager manager, NetworkList<ulong> playerOrders)
    {
        // Phased: Start with Night phase
        StartPhase(manager, GamePhase.Night);
    }

    public override void StartGame(NetworkBoardManager manager)
    {
        // Phased: Begin with Night
        StartNightPhase(manager);
    }

    private void StartNightPhase(NetworkBoardManager manager)
    {
        _currentNightOrder = GetRandomTurnOrder(manager.GetPlayerOrders());
        manager.SetTurnOrder(_currentNightOrder);
        _currentTurnIndex = 0;
        manager.SetCurrentPhase(GamePhase.Night);
        manager.StartNextTurn(); // Start first turn in random order
    }

    public override void HandlePlayerTurnStart(NetworkBoardManager manager, ulong playerId)
    {
        manager.RequestNextTurn(playerId);
    }

    public override void HandlePlayerActionComplete(NetworkBoardManager manager)
    {
        _currentTurnIndex++;
        if (_currentTurnIndex >= _currentNightOrder.Count)
        {
            // End Night, start Day
            EndPhase(manager, GamePhase.Night);
            StartPhase(manager, GamePhase.DayDiscussion);
            manager.StartCoroutine(DayDiscussionRoutine(manager));
        }
        else
        {
            // Next turn in Night
            manager.StartNextTurn();
        }
    }

    private IEnumerator DayDiscussionRoutine(NetworkBoardManager manager)
    {
        // 60s timer (sync via RPC if needed)
        yield return new WaitForSeconds(_dayDiscussionTime);
        EndPhase(manager, GamePhase.DayDiscussion);
        StartPhase(manager, GamePhase.DayVoting);
        manager.StartCoroutine(VotingCoroutine(manager));
    }

    private IEnumerator VotingCoroutine(NetworkBoardManager manager)
    {
        yield return new WaitForSeconds(3f);
        OnVotingComplete(manager);
    }

    // Call this after voting (e.g., from UI or timer)
    public void OnVotingComplete(NetworkBoardManager manager)
    {
        // Process votes (e.g., eliminate player)
        // Then resume Night
        EndPhase(manager, GamePhase.DayVoting);
        StartNightPhase(manager);
    }

    public override bool CheckEndGameConditions(NetworkBoardManager manager, out bool isDogWin)
    {
        isDogWin = true;
        if (manager.CheckForOutOfCards())
        {
            isDogWin = false;
            return true;
        }
        return false;
    }

    public override void EndGame(NetworkBoardManager manager, bool isDogWin)
    {
        // Similar to Classic
        manager.BroadcastGameEnd(isDogWin);
    }
}