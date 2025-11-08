using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class Seat : NetworkBehaviour, IInteractable
{
    [SerializeField] private Vector3 _sitOffset;
    [SerializeField] private Vector3 _sitDirection = new(0, 0, -1);
    private PlayerController _localOccupant;

    private NetworkVariable<ulong> _occupyingClientId = new NetworkVariable<ulong>(
        ulong.MaxValue, // Indicates no client (empty seat)
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public event Action<PlayerEnterSeatEventArg> OnPlayerEnterSeat;
    public struct PlayerEnterSeatEventArg : INetworkSerializable
    {
        public ulong OldClientId, NewClientId;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref OldClientId);
            serializer.SerializeValue(ref NewClientId);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initialize seat as empty
            _occupyingClientId.Value = ulong.MaxValue;
        }
        _occupyingClientId.OnValueChanged += OnOccupyingClientChanged;

        // late joiners will use NetworkVariable _occupyingClientId to sync gameObject.layer
        gameObject.layer = _occupyingClientId.Value == ulong.MaxValue ? Constant.INTERACTABLE_LAYER : Constant.IGNORE_LAYER;
    }

    public async void Interact(PlayerController actor)
    {
        await Task.Yield();
        _localOccupant = actor;
        RequestSeatServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSeatServerRpc(ulong requesterId)
    {
        if (_occupyingClientId.Value != ulong.MaxValue)
        {
            Debug.Log("some-one wants to seat on an occupying seat");
        }
        else
        {
            gameObject.layer = Constant.IGNORE_LAYER; // fast Disable interactions on server
            _occupyingClientId.Value = requesterId;
            OccupySeatClientRpc(requesterId);
        }
    }

    [ClientRpc]
    private void OccupySeatClientRpc(ulong requesterId)
    {
        gameObject.layer = Constant.IGNORE_LAYER; // Disable interactions on clients
        if (requesterId == NetworkManager.Singleton.LocalClientId && _localOccupant != null)
        {
            _localOccupant.Sit(this);
        }
    }

    public void OnExitSeat()
    {
        ExitSeatServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExitSeatServerRpc(ulong clientId)
    {
        // Check if client is occupying this seat
        if (_occupyingClientId.Value != clientId)
        {
            Debug.LogWarning($"Client {clientId} is not occupying this seat.");
            return;
        }

        // Free seat
        _occupyingClientId.Value = ulong.MaxValue;
        _localOccupant = null;
    }

    private void OnOccupyingClientChanged(ulong oldClientId, ulong newClientId)
    {
        bool isEnterSeat = newClientId == ulong.MaxValue;
        gameObject.layer =  isEnterSeat?
            Constant.INTERACTABLE_LAYER : Constant.IGNORE_LAYER;
        OnPlayerEnterSeat?.Invoke(new PlayerEnterSeatEventArg()
        {
            OldClientId = oldClientId,
            NewClientId = newClientId
        });
    }

    public Vector3 SitPosition() => transform.position + transform.TransformDirection(_sitOffset);
    public Vector3 SitPosition(Vector3 seatPos, Quaternion seatRot) => seatPos + seatRot * _sitOffset;
    public Quaternion SitRotation() => transform.rotation * Quaternion.LookRotation(_sitDirection);
    public bool IsOccupied() => _occupyingClientId.Value != ulong.MaxValue;
    public ulong GetOccupyingClientId() => _occupyingClientId.Value;
    public PlayerController GetOccupant() => _localOccupant;
}

