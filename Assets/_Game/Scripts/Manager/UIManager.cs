using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : SingletonMono<UIManager>
{
    [Header("Properties")]
    [SerializeField] private List<UIViewState> _uiViewPrefabs = new();
    private List<UIViewState> _uiViewStates = new();

    public void ShowUI(EUIState state)
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
        foreach (var viewState in _uiViewStates)
        {
            if (viewState.State == state)
            {
                viewState.View.Show();
            }
        }
    }
    public void HideUI(EUIState state)
    {
        foreach (var viewState in _uiViewStates)
        {
            if (viewState.State == state)
            {
                viewState.View.Hide();
            }
        }
    }
}

public enum EUIState
{
    None,
    MainMenu,
    CustomGame,
    InGame,
}
[Serializable]
public struct UIViewState
{
    public EUIState State;
    public UIView View;
}
