using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class UIInGameVoting : UIView
{
    [Header("References")]
    [SerializeField] private List<UIVotingMember> _uiMembers = new();
    [SerializeField] private Button _skipBTN;
    [SerializeField] private TextMeshProUGUI _skipCountTMP;
    [SerializeField] private Image _skipImage;
    private Dictionary<string, UIVotingMember> _votingMemberDict = new();

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
        _skipBTN.onClick.AddListener(TriggerSkip);
        _skipImage.gameObject.SetActive(false);
    }
    void OnDisable()
    {
        GameplayManager.Instance.OnChangedVotingData -= UpdateUI;
        GameplayManager.Instance.OnEndPhase -= HandleEndGamePhase;
        _skipBTN.onClick.RemoveListener(TriggerSkip);
    }
    private void Initialize()
    {
        _votingMemberDict.Clear();
        Lobby lobby = LobbyManager.Instance.currentLobby;
        int currentNumOfPlayer = lobby.Players.Count;
        string mineId = AuthenticationService.Instance.PlayerId;
        for (int i = 0; i < Constant.MAX_PLAYERS; i++)
        {
            _uiMembers[i].ResetMemberData();
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
                _votingMemberDict[lobby.Players[i].Id] = _uiMembers[i];
            }
            _skipBTN.interactable = true;
            _skipCountTMP.text = "0";
        }
    }
    private void UpdateUI()
    {
        List<GameplayManager.VotingData> votingDatas = GameplayManager.Instance.GetVotingDatas();
        if (votingDatas == null)
        {
            Debug.LogError("Missing voting datas!");
            return;
        };
        int skipCount = 0;
        foreach (GameplayManager.VotingData votingData in votingDatas)
        {
            if (votingData.Skip) skipCount++;
            else _votingMemberDict[votingData.ToPlayer].TakeVote(votingData.FromPlayer);
        }
        _skipCountTMP.text = skipCount.ToString();
    }
    public void TriggerVoting(string toAuthId)
    {
        _skipBTN.interactable = false;
        GameplayManager.Instance.TriggerVoting(toAuthId);
        foreach (var uiMember in _uiMembers)
        {
            uiMember.SetLock(true);
        }
    }
    private void TriggerSkip()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        _skipBTN.interactable = false;
        GameplayManager.Instance.TriggerVoting("", true);
        foreach (var uiMember in _uiMembers)
        {
            uiMember.SetLock(true);
        }
    }

    private async void HandleEndGamePhase(GamePhase phase)
    {
        if (phase != GamePhase.DayVoting) return;
        string votedPlayerId = GameplayManager.Instance.GetVotingResult();
        if (votedPlayerId == null)
        {
            _skipImage.gameObject.SetActive(true);
        }
        else
        {
            _votingMemberDict[votedPlayerId].SetActiveVotedImage(true);
        }
        await Task.Delay(2000);
        Hide();
    }
}
