using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "MyGame/GameConfig", order = 1)]
public class GameConfig : SingletonScriptableObject<GameConfig>
{

    [Header("UI View Settings")]
    public List<UIViewState> uiViewPrefabs = new();
}