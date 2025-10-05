using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "MyGame/GameConfig", order = 1)]
public class GameConfig : ScriptableObject
{
    private static GameConfig _instance;

    public static GameConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfig>("GameConfig");
                if (_instance == null)
                {
                    Debug.LogError("GameConfig asset not found in Resources!");
                }
            }
            return _instance;
        }
    }

    [Header("UI View Settings")]
    public List<UIViewState> uiViewPrefabs = new();
}