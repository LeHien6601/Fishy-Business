using System;
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

    [Serializable]
    public class SoundMapping
    {
        public SoundType soundType;
        public SoundData soundData;
    }

    [Header("Sound Mapping")]
    public List<SoundMapping> soundMappings = new List<SoundMapping>();

    [Header("Game Modes")]
    public List<GameMode> GameModes = new();
    [Header("Color list")]
    public List<Color> ColorList = new();

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
    public Color GetColor(int index)
    {
        if (index < 0 || index >= ColorList.Count) return Color.blue;
        return ColorList[index];
    }
    public string GetColorCode(int index)
    {
        if (index < 0 || index >= ColorList.Count) return ColorUtility.ToHtmlStringRGB(Color.blue);
        return ColorUtility.ToHtmlStringRGB(ColorList[index]);
    }
    #endregion
}