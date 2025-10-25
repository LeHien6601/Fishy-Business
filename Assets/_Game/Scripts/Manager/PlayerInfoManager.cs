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
    }

    public void GenerateRandomPlayerInfo()
    {
        PlayerName = Utils.GetRandomPlayerName();
        PlayerIconId = UnityEngine.Random.Range(0, GameConfig.Instance.playerIcons.Count);
        Debug.Log($"Generated Player Name: {PlayerName}, Icon ID: {PlayerIconId}");
    }
    public void UpdatePlayerInfo(string newName, int newIconId)
    {
        PlayerName = newName;
        PlayerIconId = newIconId;
        OnChangedPlayerInfo?.Invoke(new ChangedPlayerInfoEventArgs
        {
            NewPlayerName = newName,
            NewPlayerIconId = newIconId
        });
        Debug.Log($"Updated Player Info: Name - {PlayerName}, Icon ID - {PlayerIconId}");
    }
}