using System;
using UnityEngine;

public class BoardGameEffectHandler : MonoBehaviour
{

    [SerializeField] private GameObject _bombPrefab;
    [SerializeField] private float _bombSize;
    [SerializeField] private float _upOffset;
    [SerializeField] private NetworkBoardManager _source;

    void OnEnable()
    {
        _source.BombEvent += SpawnBomb;
    }

    void OnDisable()
    {
        _source.BombEvent -= SpawnBomb;
    }

    private void SpawnBomb(Vector3 arg0)
    {
        if (_bombPrefab == null)
            return;
        GameObject bomb = Instantiate(_bombPrefab, transform);
        bomb.transform.position = arg0 + Vector3.up * _upOffset;
        bomb.transform.localScale = Vector3.one * _bombSize;
        this.WaitThenExecute(1, () => Destroy(bomb));
    }
}