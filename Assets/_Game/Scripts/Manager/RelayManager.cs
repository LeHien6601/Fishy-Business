using System.Collections.Generic;
using System.Threading.Tasks;
using HHDCore;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;

public class RelayManager : SingletonMono<RelayManager>
{
    public async Task<Lobby> SetupRelay(Lobby lobby)
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(Constant.MAX_PLAYERS - 1);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Dictionary<string, DataObject> updatedData = lobby.Data;
            updatedData[Constant.KEY_RELAY_JOIN_CODE] = new DataObject(DataObject.VisibilityOptions.Public, joinCode);
            updatedData[Constant.KEY_LOBBY_CODE] = new DataObject(DataObject.VisibilityOptions.Public, lobby.LobbyCode);
            await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = updatedData
            });
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);
            NetworkManager.Singleton.StartHost();
            lobby = await LobbyService.Instance.GetLobbyAsync(lobby.Id);
            return lobby;
        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
            return null;
        }
    }

    public async Task JoinRelay(Lobby lobby)
    {
        try
        {
            var joinCode = lobby.Data[Constant.KEY_RELAY_JOIN_CODE].Value;
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);
            NetworkManager.Singleton.StartClient();

            Debug.Log("Joined Relay");
        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
        }
    }
}
