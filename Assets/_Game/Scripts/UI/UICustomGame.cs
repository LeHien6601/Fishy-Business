using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class UICustomGame : UIView
{
    #region Properties 
    [Header("References")]
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _createBtn;
    [SerializeField] private Button _joinBtn;
    [SerializeField] private RectTransform _lobbyListContent;
    [SerializeField] private UILobbyItem _lobbyItemPrefab;
    private Coroutine _refreshLobbyListCoroutine;
    private string _selectedLobbyCode = "";
    private string _selectedRelayJoinCode = "";
    private List<UILobbyItem> _lobbyItems = new();
    private int countClick = 0;
    #endregion
    private void Awake()
    {
        _backBtn.onClick.AddListener(Back);
        _createBtn.onClick.AddListener(Create);
        _joinBtn.onClick.AddListener(Join);
    }

    void OnEnable()
    {
        _joinBtn.interactable = false;
        _refreshLobbyListCoroutine = StartCoroutine(RefreshLobbyList());
        LobbyManager.Instance.OnUpdatedLobbyList += HandleChangeLobbyList;
    }
    void OnDisable()
    {
        LobbyManager.Instance.OnUpdatedLobbyList -= HandleChangeLobbyList;
        if (_refreshLobbyListCoroutine != null)
        {
            StopCoroutine(_refreshLobbyListCoroutine);
            _refreshLobbyListCoroutine = null;
        }
    }
    public override void HideWithParams(object isCreating)
    {
        base.HideWithParams(isCreating);
        if ((bool)isCreating)
        {
            UIManager.Instance.HideUI(EUIState.Footer);
        }
    }

    
    #region Lobby List Handling
    private IEnumerator RefreshLobbyList()
    {
        while (true)
        {
            yield return LobbyManager.Instance.QueryLobbiesAsync();
            yield return Utils.GetWaitForSeconds(2f);
        }
    }
    private void HandleChangeLobbyList(LobbyManager.UpdatedLoobyListEventArgs args)
    {
        // Update lobby list UI
        Debug.Log("Lobby list updated: " + args.LobbyList.Count + " lobbies available.");
        foreach (var lobby in args.LobbyList)
        {
            Debug.Log($"Lobby ID: {lobby.Id}, Name: {lobby.Name}, Players: {lobby.Players.Count}/{lobby.MaxPlayers}");
            Debug.Log($"Lobby code: {lobby.LobbyCode} Relay join code: {lobby.Data[Constant.KEY_RELAY_JOIN_CODE].Value}");
        }
        HandleChangeLobbyListUI(args.LobbyList);
    }
    private void HandleChangeLobbyListUI(List<Lobby> lobbyList)
    {
        Debug.Log("Updating lobby list UI");
        if (_lobbyItems.Count < lobbyList.Count)
        {
            Debug.Log("Need to create more lobby items");
            int toCreate = lobbyList.Count - _lobbyItems.Count;
            for (int i = 0; i < toCreate; i++)
            {
                var item = Instantiate(_lobbyItemPrefab, _lobbyListContent);
                item.OnClickedLobbyItem += HandleClickLobbyItem;
                item.OnDeselectedLobbyItem += HandleDeselectLobbyItem;
                _lobbyItems.Add(item);
            }
        }
        else if (_lobbyItems.Count > lobbyList.Count)
        {
            Debug.Log("Need to remove some lobby items");
            int toRemove = _lobbyItems.Count - lobbyList.Count;
            for (int i = 0; i < toRemove; i++)
            {
                var item = _lobbyItems[^1];
                item.OnClickedLobbyItem -= HandleClickLobbyItem;
                item.OnDeselectedLobbyItem -= HandleDeselectLobbyItem;
                Destroy(item.gameObject);
                _lobbyItems.RemoveAt(_lobbyItems.Count - 1);
            }
        }
        Debug.Log("Updating lobby items");
        for (int i = 0; i < lobbyList.Count; i++)
        {
            _lobbyItems[i].SetLobbyInfo(lobbyList[i]);
        }
        if (!lobbyList.Any(l => l.Data[Constant.KEY_LOBBY_CODE].Value == _selectedLobbyCode))
        {
            _joinBtn.interactable = false;
        }
    }

    private void HandleClickLobbyItem(UILobbyItem.ClickedLobbyItemEventArgs args)
    {
        countClick++;
        _joinBtn.interactable = true;
        _selectedLobbyCode = args.LobbyCode;
        _selectedRelayJoinCode = args.RelayJoinCode;
        Debug.Log($"Selected Lobby Code: {_selectedLobbyCode}, Relay Join Code: {_selectedRelayJoinCode}");
        SoundManager.Play2D(SoundType.ButtonClick);
    }
    private async void HandleDeselectLobbyItem()
    {
        await Task.Delay(500);
        countClick--;
        if (countClick == 0)
            _joinBtn.interactable = false;
    }
    #endregion

    #region Behaviors
    private void Back()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        UIManager.Instance.ShowUI(EUIState.MainMenu);
        UIManager.Instance.HideUI(EUIState.CustomGame, false);
    }
    private async void Create()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        UIManager.Instance.HideUI(EUIState.CustomGame, true);
        UIManager.Instance.ShowUI(EUIState.Loading);
        await LobbyManager.Instance.CreateLobbyAsync(Utils.GetRandomLobbyName());
        GameManager.Instance.StartGame();
        await Task.Delay(500);
        UIManager.Instance.HideUI(EUIState.Loading);
    }

    private async void Join()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        if (!string.IsNullOrEmpty(_selectedLobbyCode))
        {
            UIManager.Instance.HideUI(EUIState.CustomGame, true);
            UIManager.Instance.ShowUI(EUIState.Loading);
            await LobbyManager.Instance.JoinLobbyByCodeAsync(_selectedLobbyCode, PlayerInfoManager.Instance.PlayerName, PlayerInfoManager.Instance.PlayerIconId);
            await Task.Delay(2500);
            UIManager.Instance.HideUI(EUIState.Loading);
        }
        else
        {
            Debug.LogWarning("No lobby selected to join.");
        }
    }
    #endregion
}
