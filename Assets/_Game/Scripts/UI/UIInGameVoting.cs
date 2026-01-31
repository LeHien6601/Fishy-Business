using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class UIInGameVoting : UIView
{
    [Header("References")]
    [SerializeField] private List<UIVotingMember> _uiMembers = new();
    private Dictionary<ulong, UIVotingMember> _votingMemberDict = new();

    public override void Show()
    {
        Initialize();
        base.Show();
    }
    public override void Hide()
    {
        base.Hide();
    }
    void OnEnable()
    {
        GameplayManager.Instance.OnChangedVotingData += UpdateUI;
        GameplayManager.Instance.OnEndPhase += HandleEndGamePhase;
    }
    void OnDisable()
    {
        GameplayManager.Instance.OnChangedVotingData -= UpdateUI;
        GameplayManager.Instance.OnEndPhase -= HandleEndGamePhase;
    }
    private void Initialize()
    {
        _votingMemberDict.Clear();
        Lobby lobby = LobbyManager.Instance.currentLobby;
        int currentNumOfPlayer = lobby.Players.Count;
        string mineId = AuthenticationService.Instance.PlayerId;
        for (int i = 0; i < Constant.MAX_PLAYERS; i++)
        {
            if (i < currentNumOfPlayer)
            {
                _uiMembers[i].SetUIInGameVoting(this);
                _uiMembers[i].SetMemberData(
                    mineId == lobby.Players[i].Id,
                    lobby.Players[i].Data[Constant.KEY_PLAYER_NAME].Value,
                    lobby.Players[i].Data[Constant.KEY_PLAYER_ICON_ID].Value != null ?
                        GameConfig.Instance.GetPlayerIconById(
                            int.Parse(lobby.Players[i].Data[Constant.KEY_PLAYER_ICON_ID].Value)) : null,
                    lobby.Players[i].Id);
                _uiMembers[i].SetLock(false);
                GameManager.Instance.GetNetIdByAuthId(lobby.Players[i].Id, out var id);
                _votingMemberDict[id] = _uiMembers[i];
            }
            else {
                _uiMembers[i].ResetMemberData();
            }
        }
    }
    private void UpdateUI()
    {
        List<GameplayManager.VotingData> votingDatas = GameplayManager.Instance.GetVotingDatas();
        if (votingDatas == null) return;
        foreach (GameplayManager.VotingData votingData in votingDatas)
        {
            _votingMemberDict[votingData.ToPlayer].TakeVote(votingData.FromPlayer);
        }
    }
    public void TriggerVoting(string toAuthId)
    {
        if (!GameManager.Instance.GetNetIdByAuthId(toAuthId, out var netId))
        {
            Debug.LogError("Missing player net id in GameManager dictionary!");
            return;
        }
        GameplayManager.Instance.TriggerVoting(netId);
        foreach (var uiMember in _uiMembers)
        {
            uiMember.SetLock(true);
        }
    }

    private void HandleEndGamePhase(GamePhase phase)
    {
        if (phase == GamePhase.DayVoting) Hide();
    }
}
