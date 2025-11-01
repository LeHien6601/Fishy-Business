// using Unity.Netcode;
// using UnityEngine;

// /// <summary>
// /// unity requires: only server can reparent network objects
// /// </summary>
// public class ReparentHandler : SingletonMonoNet<ReparentHandler>
// {
//     [ServerRpc(RequireOwnership = false)]
//     public void RequestReparentServerRpc(ulong objectToMoveId, ulong newParentId = 0, ServerRpcParams rpcParams = default)
//     {
//         var objectToMove = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectToMoveId];

//         if (objectToMove == null) return;

//         // Unparent if no new parent specified
//         if (newParentId == 0)
//         {
//             objectToMove.transform.SetParent(null);
//             // NotifyReparentClientRpc(objectToMoveId, null);
//             return;
//         }

//         // Reparent if new parent provided
//         var newParent = NetworkManager.Singleton.SpawnManager.SpawnedObjects[newParentId];
//         if (newParent != null)
//         {
//             objectToMove.transform.SetParent(newParent.transform);
//             // NotifyReparentClientRpc(objectToMoveId, newParentId);
//         }
//     }

//     [ClientRpc]
//     private void NotifyReparentClientRpc(ulong objectId, ulong parentId)
//     {
//         // Optional: Update local UI, effects, etc.
//         // Debug.Log($"Object {objectId} {(parentId.HasValue ? "reparented" : "unparented")}");
//     }
// }