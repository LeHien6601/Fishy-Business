using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> _mapObjects;
    void Awake()
    {
        foreach (var map in _mapObjects)
        {
            map.SetActive(false);
        }
        GameData gameData = JsonUtility.FromJson<GameData>(LobbyManager.Instance.currentLobby.Data[Constant.KEY_GAME_MODE_DATA].Value);
        if (gameData.MapIndex > -1 && gameData.MapIndex < _mapObjects.Count)
        {
            _mapObjects[gameData.MapIndex].SetActive(true);
        }
    }
}