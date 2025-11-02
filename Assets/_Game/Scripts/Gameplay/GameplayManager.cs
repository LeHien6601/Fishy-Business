using System.Collections.Generic;
using System.Linq;
using HHDCore;
using Unity.Netcode;

public class GameplayManager : SingletonMonoNet<GameplayManager>
{
    private List<LobbyManager.PlayerInfo> _winerInfos = new();
    //Server
    public void HandleEndGame(List<ulong> winerIds, List<ulong> playerIds)
    {
        _winerInfos.Clear();
        List<string> winerAuthIds = winerIds.Select(id => GameManager.Instance.GetAuthIdByNetId(id)).ToList();
        foreach (var player in LobbyManager.Instance.currentLobby.Players)
        {
            if (winerAuthIds.Contains(player.Id))
            {
                LobbyManager.PlayerInfo playerInfo = new()
                {
                    Name = player.Data[Constant.KEY_PLAYER_NAME].Value,
                    IconId = int.Parse(player.Data[Constant.KEY_PLAYER_ICON_ID].Value, 0)
                };
                _winerInfos.Add(playerInfo);
            }
        }
        UpdateWinerInfosClientRpc(_winerInfos.ToArray(), playerIds.ToArray());
    }
    [ClientRpc]
    private void UpdateWinerInfosClientRpc(LobbyManager.PlayerInfo[] winerInfos, ulong[] playerIds)
    {
        if (!playerIds.Contains(NetworkManager.Singleton.LocalClientId)) return;
        _winerInfos = winerInfos.ToList();
        UIManager.Instance.ShowUI(EUIState.EndGame);
    }
}