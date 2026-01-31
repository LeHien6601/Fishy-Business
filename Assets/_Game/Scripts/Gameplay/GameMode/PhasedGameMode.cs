using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "PhasedGameMode", menuName = "GameModes/Phased")]
public class PhasedGameMode : GameMode
{
    [SerializeField] private float _dayDiscussionTime = 10f;
    [SerializeField] private float _votingTime = 5f;

    private List<ulong> _currentNightOrder = new();
    private int _currentTurnIndex;

    public override void StartPhase(NetworkBoardManager manager, GamePhase phase)
    {
        base.StartPhase(manager, phase);
        Debug.Log("Start phase " + phase.ToString());
        if (phase == GamePhase.Night)
        {
            manager.CoverAllClientsRpc();
        }
        else
        {
            manager.UncoverAllClientsRpc();
        }
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
        GameplayManager.Instance.TriggerStartPhase(GamePhase.Night, 0f);
    }

    public override void HandlePlayerTurnStart(NetworkBoardManager manager, ulong playerId)
    {
        manager.CoverAllClientsRpc();

        manager.RequestNextTurn(playerId);
        
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { playerId } // Send to specific player only
            }
        };
        manager.UnCoverASpecificClientRpc(clientRpcParams);
    }

    public override void HandlePlayerActionComplete(NetworkBoardManager manager)
    {
        _currentTurnIndex++;
        if (_currentTurnIndex >= _currentNightOrder.Count)
        {
            // End Night, start Day
            EndPhase(manager, GamePhase.Night);
            StartPhase(manager, GamePhase.DayDiscussion);
            GameplayManager.Instance.TriggerStartPhase(GamePhase.DayDiscussion, _dayDiscussionTime);
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
        GameplayManager.Instance.TriggerStartPhase(GamePhase.DayVoting, _votingTime);
        manager.StartCoroutine(VotingCoroutine(manager));
    }

    private IEnumerator VotingCoroutine(NetworkBoardManager manager)
    {
        yield return new WaitForSeconds(_votingTime);
        OnVotingComplete(manager);
    }

    // Call this after voting (e.g., from UI or timer)
    public void OnVotingComplete(NetworkBoardManager manager)
    {
        // Process votes (e.g., eliminate player)
        // Then resume Night
        EndPhase(manager, GamePhase.DayVoting);
        StartPhase(manager, GamePhase.Night);
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