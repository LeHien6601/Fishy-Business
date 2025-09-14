using UnityEngine;

public class GameManager : SingletonMono<GameManager>
{
    public string PlayerName { get; private set; }
    public int PlayerIconID { get; private set; }

    void Start()
    {
        PlayerName = Utils.GetRandomPlayerName();
        PlayerIconID = Random.Range(0, 20); // Assuming there are 20 player icons
        Debug.Log($"Player Name: {PlayerName}, Icon ID: {PlayerIconID}");
    }
}
