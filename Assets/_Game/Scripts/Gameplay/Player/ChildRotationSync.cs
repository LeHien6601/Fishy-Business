using Unity.Netcode;
using UnityEngine;

public class ChildRotationSync : NetworkBehaviour
{
    // 1. Reference to the child Transform whose rotation you want to sync
    [SerializeField] private Transform _childToSync;

    // 2. NetworkVariable to synchronize the rotation value
    private readonly NetworkVariable<Quaternion> _syncedRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner // Only the owner can change the rotation
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Apply the initial rotation immediately for late joiners
        if (!IsOwner)
        {
            _childToSync.rotation = _syncedRotation.Value;
        }
    }
    private void LateUpdate()
    {
        // 3. Update the NetworkVariable on the owner/controlling client
        if (IsOwner)
        {
            // Only update if the rotation has actually changed to save bandwidth
            if (_childToSync.rotation != _syncedRotation.Value)
            {
                _syncedRotation.Value = _childToSync.rotation;
            }
        }
        else
        {
            _childToSync.rotation = _syncedRotation.Value; // this line runs locally on non-owners, not affect the network
        }
    }
}