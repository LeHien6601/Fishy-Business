using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HHDCore;
using Unity.Netcode;
using UnityEditor;

public class GameplayManager : SingletonMonoNet<GameplayManager>
{
    private List<LobbyManager.PlayerInfo> _winnerInfos = new(); //Only handle for 1 board!
    private PlayerRole _playerRole;
    public event Action<ulong> OnStartGame;
    public event Action<ulong> OnEndGame;
    public event Action<StartedNewTurnEventArgs> OnStartedNewTurn;
    public event Action OnResetGame;
    public event Action<ActionCardType, ToolType> OnUseActionCard;
    public struct StartedNewTurnEventArgs
    {
        public ulong ClientId;
        public int TurnNumber;
    }
    public event Action<StartPhaseEventArgs> OnStartPhase;
    public struct StartPhaseEventArgs
    {
        public GamePhase Phase;
        public float Duration;
        public List<object> AdditionalData;
    }

    #region HANDLERS
    public void TriggerStartGame(ulong id)
    {
        OnStartGame?.Invoke(id);
        SoundManager.PlayMusic(SoundType.Gameplay);
    }
    public void TriggerEndGame(ulong id)
    {
        OnEndGame?.Invoke(id);
        SoundManager.PlayMusic(SoundType.Lobby, 2f);
    }
    public void TriggerStartPhase(GamePhase phase, float duration, List<object> additionalData = null)
    {
        OnStartPhase?.Invoke(new StartPhaseEventArgs()
        {
            Phase = phase,
            Duration = duration,
            AdditionalData = additionalData
        });
    }
    //Server-Start game
    public void HandleStartGame(Dictionary<ulong, PlayerRole> playerRoleMap)
    {
        foreach (var pair in playerRoleMap)
        {
            UpdatePlayerRoleClientRpc(pair.Key, pair.Value);
        }
    }
    [ClientRpc]
    private void UpdatePlayerRoleClientRpc(ulong cliendId, PlayerRole role)
    {
        if (cliendId != NetworkManager.Singleton.LocalClientId) return;
        _playerRole = role;
        UIManager.Instance.ShowUI(EUIState.InGame);
    }

    public void HandleResetGame(List<ulong> idList)
    {
        foreach (var id in idList)
        {
            ResetPlayerClientRpc(id);
        }
    }
    [ClientRpc]
    private void ResetPlayerClientRpc(ulong cliendId)
    {
        if (cliendId != NetworkManager.Singleton.LocalClientId) return;
        _playerRole = PlayerRole.Unknown;
        UIManager.Instance.HideUI(EUIState.InGame);
    }

    //Server-End game
    public void TriggerResetGame()
    {
        if (!IsHost) return;
        OnResetGame?.Invoke();
    }
    public async void HandleEndGame(List<ulong> winnerIds, List<ulong> playerIds)
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
        await Task.Delay((int)(1000 * Constant.RESTART_INTERVAL + 1000));
        OnResetGame?.Invoke();
    }
    [ClientRpc]
    private void UpdateWinnerInfosClientRpc(LobbyManager.PlayerInfo[] winnerInfos, ulong[] playerIds)
    {
        if (!playerIds.Contains(NetworkManager.Singleton.LocalClientId)) return;
        _winnerInfos = winnerInfos.ToList();
        UIManager.Instance.ShowUI(EUIState.EndGame);
    }

    public void HandleNewTurn(ulong cliendId, int turnNumber)
    {
        OnStartedNewTurn?.Invoke(new StartedNewTurnEventArgs()
        {
            ClientId = cliendId,
            TurnNumber = turnNumber
        });
    }
    public void TriggerActionCard(ActionCardType actionCardType, ToolType toolType)
    {
        OnUseActionCard?.Invoke(actionCardType, toolType);
    }
    #endregion

    #region GETTERS
    public List<LobbyManager.PlayerInfo> GetWinnerInfos() { return _winnerInfos; }
    public PlayerRole GetPlayerRole()
    {
        return _playerRole;
    }
    #endregion
}