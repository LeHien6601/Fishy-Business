// using System.Collections;
// using System.Collections.Generic;
// using Unity.Netcode;
// using UnityEngine;

// [CreateAssetMenu(fileName = "PhasedGameMode", menuName = "GameModes/Phased")]
// public class PhasedGameMode : GameMode
// {
//     [SerializeField] private float dayDiscussionTime = 60f;

//     private List<ulong> currentNightOrder;
//     private int currentTurnIndex;

//     public override void Initialize(NetworkBoardManager manager, NetworkList<ulong> playerOrders)
//     {
//         // Phased: Start with Night phase
//         StartPhase(manager, GamePhase.Night);
//     }

//     public override void StartGame(NetworkBoardManager manager)
//     {
//         // Phased: Begin with Night
//         StartNightPhase(manager);
//     }

//     private void StartNightPhase(NetworkBoardManager manager)
//     {
//         currentNightOrder = GetRandomTurnOrder(manager.GetPlayerOrders());
//         currentTurnIndex = 0;
//         manager.SetCurrentPhase(GamePhase.Night);
//         manager.StartNextTurn(); // Start first turn in random order
//     }

//     public override void HandlePlayerTurnStart(NetworkBoardManager manager, ulong playerId)
//     {
//         manager.NextTurnClientRpc(playerId);
//     }

//     public override void HandlePlayerActionComplete(NetworkBoardManager manager)
//     {
//         currentTurnIndex++;
//         if (currentTurnIndex >= currentNightOrder.Count)
//         {
//             // End Night, start Day
//             EndPhase(manager, GamePhase.Night);
//             StartPhase(manager, GamePhase.DayDiscussion);
//             manager.StartCoroutine(DayDiscussionRoutine(manager));
//         }
//         else
//         {
//             // Next turn in Night
//             manager.StartNextTurn();
//         }
//     }

//     private IEnumerator DayDiscussionRoutine(NetworkBoardManager manager)
//     {
//         // 60s timer (sync via RPC if needed)
//         yield return new WaitForSeconds(dayDiscussionTime);
//         EndPhase(manager, GamePhase.DayDiscussion);
//         StartPhase(manager, GamePhase.DayVoting);
//         // Start voting (implement your voting logic here, e.g., open UI, collect votes via RPC)
//         // For example: manager.StartVoting();
//         // Assume voting completes via a callback: OnVotingComplete(manager);
//     }

//     // Call this after voting (e.g., from UI or timer)
//     public void OnVotingComplete(NetworkBoardManager manager)
//     {
//         // Process votes (e.g., eliminate player)
//         // Then resume Night
//         EndPhase(manager, GamePhase.DayVoting);
//         StartNightPhase(manager);
//     }

//     public override bool CheckEndGameConditions(NetworkBoardManager manager, out bool isDogWin)
//     {
//         // Similar to Classic, but add phase-specific checks (e.g., all cats voted out)
//         return base.CheckEndGameConditions(manager, out isDogWin); // Or override
//     }

//     public override void EndGame(NetworkBoardManager manager, bool isDogWin)
//     {
//         // Similar to Classic
//         base.EndGame(manager, isDogWin);
//     }
// }