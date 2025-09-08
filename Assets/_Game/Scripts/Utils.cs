using UnityEngine;

public class Utils : MonoBehaviour
{
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
}
