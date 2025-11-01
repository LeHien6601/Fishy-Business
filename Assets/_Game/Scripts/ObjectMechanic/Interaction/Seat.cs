using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class Seat : NetworkBehaviour, IInteractable
{
    [SerializeField] private Vector3 _sitOffset;
    [SerializeField] private Vector3 _sitDirection = new(0, 0, -1);
    private PlayerController _occupant;

    private NetworkVariable<ulong> _occupyingClientId = new NetworkVariable<ulong>(
        ulong.MaxValue, // Indicates no client (empty seat)
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initialize seat as empty
            _occupyingClientId.Value = ulong.MaxValue;
        }
        _occupyingClientId.OnValueChanged += OnOccupyingClientChanged;
    }

    public async void Interact(PlayerController actor)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            await Task.Yield();
            actor.Sit(this);
            _occupant = actor;
        }
    }

    public void OnEnterSeat()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        RequestOccupySeatServerRpc(localClientId);
    }

    public void OnExitSeat()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            RequestExitSeatServerRpc(localClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOccupySeatServerRpc(ulong clientId)
    {
        // Check if seat is already occupied
        if (_occupyingClientId.Value != ulong.MaxValue)
        {
            Debug.LogWarning($"Seat already occupied by client {_occupyingClientId.Value}.");
            _occupant = null;
            return;
        }

        // Occupy seat
        _occupyingClientId.Value = clientId;
        OnEnterClientRpc(clientId);
    }

    [ClientRpc]
    private void OnEnterClientRpc(ulong clientId)
    {
        gameObject.layer = Constant.IGNORE_LAYER; // Disable interactions
        Debug.Log($"Client {clientId} occupied seat {gameObject.name}.");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestExitSeatServerRpc(ulong clientId)
    {
        // Check if client is occupying this seat
        if (_occupyingClientId.Value != clientId)
        {
            Debug.LogWarning($"Client {clientId} is not occupying this seat.");
            return;
        }

        // Free seat
        _occupyingClientId.Value = ulong.MaxValue;
        _occupant = null;
        OnExitClientRpc(clientId);
    }

    [ClientRpc]
    private void OnExitClientRpc(ulong clientId)
    {
        gameObject.layer = Constant.INTERACTABLE_LAYER; // Re-enable interactions
        Debug.Log($"Client {clientId} left seat {gameObject.name}.");
    }

    private void OnOccupyingClientChanged(ulong oldClientId, ulong newClientId)
    {
        // Update local state (e.g., UI or visuals) when seat occupancy changes
        if (newClientId == ulong.MaxValue)
        {
            gameObject.layer = Constant.INTERACTABLE_LAYER; // Seat is empty
        }
        else
        {
            gameObject.layer = Constant.IGNORE_LAYER; // Seat is occupied
        }
    }

    public Vector3 SitPosition() => transform.position + transform.TransformDirection(_sitOffset);
    public Vector3 SitPosition(Vector3 seatPos, Quaternion seatRot) => seatPos + seatRot * _sitOffset;
    public Quaternion SitRotation() => transform.rotation * Quaternion.LookRotation(_sitDirection);
    public bool IsOccupied() => _occupyingClientId.Value != ulong.MaxValue;
    public ulong GetOccupyingClientId() => _occupyingClientId.Value;
    public PlayerController GetOccupant() => _occupant;
}