using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HHDCore;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameplayManager : SingletonMonoNet<GameplayManager>
{
    private List<LobbyManager.PlayerInfo> _winnerInfos = new(); //Only handle for 1 board!
    private PlayerRole _playerRole;
    //Server
    public void HandleStartGame(Dictionary<ulong, PlayerRole> playerRoleMap)
    {
        foreach (var pair in playerRoleMap)
        {
            UpdatePlayRoleClientRpc(pair.Key, pair.Value);
        }
    }
    [ClientRpc]
    private void UpdatePlayRoleClientRpc(ulong cliendId, PlayerRole role)
    {
        if (cliendId != NetworkManager.Singleton.LocalClientId) return;
        _playerRole = role;
        UIManager.Instance.ShowUI(EUIState.InGame);
    }
    public PlayerRole GetPlayerRole()
    {
        return _playerRole;
    }
    
    //Server
    public void HandleEndGame(List<ulong> winnerIds, List<ulong> playerIds)
    {
        _winnerInfos.Clear();
        List<string> winnerAuthIds = winnerIds.Select(id => GameManager.Instance.GetAuthIdByNetId(id)).ToList();
        foreach (var player in LobbyManager.Instance.currentLobby.Players)
        {
            if (winnerAuthIds.Contains(player.Id))
            {
                LobbyManager.PlayerInfo playerInfo = new()
                {
                    Name = player.Data[Constant.KEY_PLAYER_NAME].Value,
                    IconId = int.Parse(player.Data[Constant.KEY_PLAYER_ICON_ID].Value, 0)
                };
                _winnerInfos.Add(playerInfo);
            }
        }
        UpdateWinnerInfosClientRpc(_winnerInfos.ToArray(), playerIds.ToArray());
    }
    [ClientRpc]
    private void UpdateWinnerInfosClientRpc(LobbyManager.PlayerInfo[] winnerInfos, ulong[] playerIds)
    {
        if (!playerIds.Contains(NetworkManager.Singleton.LocalClientId)) return;
        _winnerInfos = winnerInfos.ToList();
        UIManager.Instance.ShowUI(EUIState.EndGame);
    }
    public List<LobbyManager.PlayerInfo> GetWinnerInfos() { return _winnerInfos; }
}