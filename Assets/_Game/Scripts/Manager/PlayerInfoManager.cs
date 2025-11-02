using System;
using HHDCore;
using UnityEngine;

public class PlayerInfoManager : SingletonMono<PlayerInfoManager>
{
    private string _playerName = "Player";
    private int _playerIconId = 0;

    public string PlayerName
    {
        get => _playerName;
        set => _playerName = value;
    }

    public int PlayerIconId
    {
        get => _playerIconId;
        set => _playerIconId = value;
    }

    public event Action<ChangedPlayerInfoEventArgs> OnChangedPlayerInfo;
    public struct ChangedPlayerInfoEventArgs
    {
        public string NewPlayerName;
        public int NewPlayerIconId;
        public ulong PlayerId;
    }

    public void GenerateRandomPlayerInfo()
    {
        // First try to load from PlayerPrefs
        if (PlayerPrefs.HasKey(Constant.KEY_PLAYER_NAME))
        {
            PlayerName = PlayerPrefs.GetString(Constant.KEY_PLAYER_NAME);
            PlayerIconId = PlayerPrefs.GetInt(Constant.KEY_PLAYER_ICON_ID, 0);
            Debug.Log($"Loaded saved player info - Name: {PlayerName}, Icon ID: {PlayerIconId}");
        }
        else
        {
            // If no saved data, generate random
            PlayerName = Utils.GetRandomPlayerName();
            PlayerIconId = UnityEngine.Random.Range(0, GameConfig.Instance.playerIcons.Count);
            
            // Save the generated info
            PlayerPrefs.SetString(Constant.KEY_PLAYER_NAME, PlayerName);
            PlayerPrefs.SetInt(Constant.KEY_PLAYER_ICON_ID, PlayerIconId);
            PlayerPrefs.Save();
            
            Debug.Log($"Generated new player info - Name: {PlayerName}, Icon ID: {PlayerIconId}");
        }
    }

    public void UpdatePlayerInfo(string newName, int newIconId)
    {
        PlayerName = newName;
        PlayerIconId = newIconId;

        // Save to PlayerPrefs
        PlayerPrefs.SetString(Constant.KEY_PLAYER_NAME, newName);
        PlayerPrefs.SetInt(Constant.KEY_PLAYER_ICON_ID, newIconId);
        PlayerPrefs.Save();

        OnChangedPlayerInfo?.Invoke(new ChangedPlayerInfoEventArgs
        {
            NewPlayerName = newName,
            NewPlayerIconId = newIconId
        });
        
        Debug.Log($"Updated and saved player info - Name: {newName}, Icon ID: {newIconId}");
    }
}