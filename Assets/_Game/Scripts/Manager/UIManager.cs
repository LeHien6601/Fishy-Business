using System;
using System.Collections.Generic;
using System.Linq;
using HHDCore;
using UnityEngine;

public class UIManager : SingletonMono<UIManager>
{
    private List<UIViewState> _uiViewPrefabs = new();
    private List<UIViewState> _uiViewStates = new();

    private void Start()
    {
        _uiViewPrefabs = GameConfig.Instance.uiViewPrefabs;
    }

    void Update()
    {
        if (true)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool isShowingLobbyUI = false;
                foreach (var viewState in _uiViewStates)
                {
                    if (viewState.State == EUIState.LobbyGameplay && viewState.View.gameObject.activeSelf)
                    {
                        isShowingLobbyUI = true;
                        break;
                    }
                }
                if (isShowingLobbyUI)
                {
                    HideUI(EUIState.LobbyGameplay, true);
                }
                else
                {
                    ShowUI(EUIState.LobbyGameplay);
                }
            }
        }
    }

    #region View Actions
    public void ShowUI(EUIState state, object param = null)
    {
        if (!_uiViewStates.Exists(v => v.State == state))
        {
            var viewPrefab = _uiViewPrefabs.Find(v => v.State == state);
            if (viewPrefab.View != null)
            {
                var viewInstance = Instantiate(viewPrefab.View, transform);
                _uiViewStates.Add(new UIViewState() { State = state, View = viewInstance });
            }
        }
        foreach (var viewState in _uiViewStates.ToList())
        {
            if (viewState.State == state)
            {
                viewState.View.SetSortingOrder(GetSortingOrder(state));
                if (param != null)
                {
                    viewState.View.ShowWithParams(param);
                }
                else
                {
                    viewState.View.Show();
                }
            }
        }
    }
    public void HideUI(EUIState state, object param = null)
    {
        foreach (var viewState in _uiViewStates)
        {
            if (viewState.State == state)
            {
                if (param != null)
                {
                    viewState.View.HideWithParams(param);
                }
                else
                {
                    viewState.View.Hide();
                }
            }
        }
    }
    #endregion

    private int GetSortingOrder(EUIState state)
    {
        var viewPrefab = _uiViewPrefabs.Find(v => v.State == state);
        if (viewPrefab.View != null)
        {
            return viewPrefab.SortingOrder;
        }
        return 0;
    }
}
public enum EUIState
{
    None,
    MainMenu,
    CustomGame,
    LobbyInfo,
    LobbyGameplay,
    Footer,
    Loading,
    PlayerInfo,
    EndGame,
}
[Serializable]
public struct UIViewState
{
    public EUIState State;
    public UIView View;
    public int SortingOrder;
}
