using System.Collections.Generic;
using System.Linq;
using HHDCore;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameplayManager : SingletonMonoNet<GameplayManager>
{
    private List<LobbyManager.PlayerInfo> _winnerInfos = new();
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
        Debug.Log(winnerIds.Count);
        foreach (var id in winnerIds) Debug.Log(id);
        Debug.Log(winnerAuthIds.Count);
        foreach (var id in winnerAuthIds) Debug.Log(id);
        Debug.Log(_winnerInfos.Count);
        foreach (var info in _winnerInfos) Debug.Log(info.Name);
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