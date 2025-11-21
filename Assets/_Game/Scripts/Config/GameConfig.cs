using System.Collections.Generic;
using HHDCore;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "MyGame/GameConfig", order = 1)]
public class GameConfig : SingletonScriptableObject<GameConfig>
{

    [Header("UI View Settings")]
    public List<UIViewState> uiViewPrefabs = new();

    [Header("Player Icon Settings")]
    public List<Sprite> playerIcons = new();

    #region Getters
    public Sprite GetPlayerIconById(int iconId)
    {
        if (iconId >= 0 && iconId < playerIcons.Count)
        {
            return playerIcons[iconId];
        }
        else
        {
            Debug.LogWarning($"Player icon ID {iconId} is out of range. Returning default icon.");
            return playerIcons.Count > 0 ? playerIcons[0] : null;
        }
    }
    #endregion
}