using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HHDCore;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEditor;
using UnityEngine;

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
    }
    public event Action<GamePhase> OnEndPhase;
    private List<VotingData> _currentVotingData = new();
    [Serializable]
    public struct VotingData
    {
        public string FromPlayer;
        public string ToPlayer;
        public bool Skip;
    }
    public event Action OnChangedVotingData;

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
    // Server trigger start phase
    public void TriggerStartPhase(GamePhase phase, float duration)
    {
        TriggerStartPhaseClientRPC(phase, duration);
    }
    public void TriggerEndPhase(GamePhase phase)
    {
        TriggerEndPhaseClientRPC(phase);
    }
    [ClientRpc]
    private void TriggerStartPhaseClientRPC(GamePhase phase, float duration)
    {
        if (phase == GamePhase.DayVoting) 
        {
            _currentVotingData.Clear();
            UIManager.Instance.ShowUI(EUIState.InGameVoting);
        }
        OnStartPhase?.Invoke(new StartPhaseEventArgs()
        {
            Phase = phase,
            Duration = duration
        });
    }
    [ClientRpc]
    private void TriggerEndPhaseClientRPC(GamePhase phasen)
    {
        OnEndPhase?.Invoke(phasen);
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

    public void TriggerVoting(string toPlayer, bool isSkip = false)
    {
        Debug.Log($"Player {AuthenticationService.Instance.PlayerId} voted {toPlayer}");
        VotingServerRpc(AuthenticationService.Instance.PlayerId, toPlayer, isSkip);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void VotingServerRpc(string from, string to, bool isSkip)
    {
        Debug.Log($"ServerRpc Player {from} voted {to}");
        VotingClientRpc(from,to,isSkip);
    }

    [ClientRpc]
    private void VotingClientRpc(string from, string to, bool isSkip)
    {
        Debug.Log($"CLientRpc Player {from} voted {to}");
        _currentVotingData.Add(new VotingData()
        {
            FromPlayer = from,
            ToPlayer = to,
            Skip = isSkip
        });
        OnChangedVotingData?.Invoke();
    }
    #endregion


    #region GETTERS
    public List<LobbyManager.PlayerInfo> GetWinnerInfos() { return _winnerInfos; }
    public PlayerRole GetPlayerRole()
    {
        return _playerRole;
    }
    public List<VotingData> GetVotingDatas() {return _currentVotingData;}
    #endregion
}