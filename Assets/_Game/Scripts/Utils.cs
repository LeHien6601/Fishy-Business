using System.Collections.Generic;
using UnityEngine;

public class Utils : MonoBehaviour
{
    public static Dictionary<float, WaitForSeconds> WaitForSecondsCache = new();
    public static WaitForSeconds GetWaitForSeconds(float seconds)
    {
        if (!WaitForSecondsCache.ContainsKey(seconds))
        {
            WaitForSecondsCache[seconds] = new WaitForSeconds(seconds);
        }
        return WaitForSecondsCache[seconds];
    }
    public static string GetRandomPlayerName()
    {
        string[] names = { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliet", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "X-ray", "Yankee", "Zulu" };
        return names[Random.Range(0, names.Length)] + Random.Range(0, 1000).ToString("000");
    }
    public static string GetRandomLobbyName()
    {
        string[] adjectives = { "Red", "Blue", "Green", "Yellow", "Fast", "Slow", "Happy", "Sad", "Bright", "Dark", "Loud", "Quiet", "Hot", "Cold", "Sharp", "Dull" };
        string[] nouns = { "Tiger", "Eagle", "Shark", "Wolf", "Lion", "Bear", "Dragon", "Phoenix", "Falcon", "Cheetah", "Panther", "Leopard" };
        return adjectives[Random.Range(0, adjectives.Length)] + nouns[Random.Range(0, nouns.Length)] + Random.Range(0, 100).ToString("00");
    }
    public static void ResetGameModeData(int index)
    {
        if (index == 0)
        {
            ClassicGameMode classicGameMode = GameConfig.Instance.GameModes[index] as ClassicGameMode;
            classicGameMode.TurnInterval = Constant.DEFAULT_TURN_INTERVAL;
        }
        else
        {
            PhasedGameMode phasedGameMode = GameConfig.Instance.GameModes[index] as PhasedGameMode;
            phasedGameMode.TurnInterval = Constant.DEFAULT_TURN_INTERVAL;
            phasedGameMode.VotingInterval = Constant.DEFAULT_VOTING_INTERVAL;
            phasedGameMode.DayDiscussionInterval = Constant.DEFAULT_DISCUSSION_INTERVAL;
        }
    }
    
    public static void UpdateGameModeData(string json)
    {
        GameData gameData = JsonUtility.FromJson<GameData>(json);
        int index = gameData.GameModeIndex;
        if (index == 0)
        {
            ClassicGameMode classicGameMode = GameConfig.Instance.GameModes[index] as ClassicGameMode;
            classicGameMode.TurnInterval = gameData.TurnInterval;
        }
        else
        {
            PhasedGameMode phasedGameMode = GameConfig.Instance.GameModes[index] as PhasedGameMode;
            phasedGameMode.TurnInterval = gameData.TurnInterval;
            phasedGameMode.VotingInterval = gameData.VotingInterval;
            phasedGameMode.DayDiscussionInterval = gameData.DayDiscussionInverval;
        }
    }
    public static string GetJsonGameModeData(int gameModeIndex, int mapIndex)
    {
        GameMode gameMode = GameConfig.Instance.GameModes[gameModeIndex];
        GameData gameData = new()
        {
            GameModeIndex = gameModeIndex,
            MapIndex = mapIndex,
            TurnInterval = gameMode.TurnInterval,
            VotingInterval = (gameModeIndex == 1) ? ((PhasedGameMode)gameMode).VotingInterval : Constant.DEFAULT_VOTING_INTERVAL,
            DayDiscussionInverval = (gameModeIndex == 1) ? ((PhasedGameMode)gameMode).DayDiscussionInterval : Constant.DEFAULT_DISCUSSION_INTERVAL
        };
        return JsonUtility.ToJson(gameData);
    }
}
