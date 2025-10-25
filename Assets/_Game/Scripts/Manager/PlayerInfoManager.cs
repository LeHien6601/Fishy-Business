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

    public void GenerateRandomPlayerInfo()
    {
        PlayerName = Utils.GetRandomPlayerName();
        PlayerIconId = Random.Range(0, GameConfig.Instance.playerIcons.Count); 
        Debug.Log($"Generated Player Name: {PlayerName}, Icon ID: {PlayerIconId}");
    }
}