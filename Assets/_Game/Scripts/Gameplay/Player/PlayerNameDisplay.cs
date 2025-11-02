using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerNameDisplay : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _nameContainerTransform;
    [SerializeField] private TextMeshProUGUI _nameTMP;
    [SerializeField] private Canvas _canvas;
    private Camera _camera;

    // Replace string NetworkVariable with FixedString128Bytes
    private NetworkVariable<FixedString128Bytes> _playerName = new(new FixedString128Bytes(""), 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _playerName.OnValueChanged += HandleChangeName;
        _camera = Camera.main;
        _canvas.worldCamera = _camera;
        _nameTMP.text = _playerName.Value.ToString();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _playerName.OnValueChanged -= HandleChangeName;
    }

    // Update handler to use FixedString128Bytes
    private void HandleChangeName(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        _nameTMP.text = newValue.ToString();
    }

    // Update SetPlayerName to use FixedString128Bytes
    public void SetPlayerName(string name)
    {
        _playerName.Value = new FixedString128Bytes(name);
    }

    void LateUpdate()
    {
        if (_camera == null) return;
        _nameContainerTransform.LookAt(_camera.transform);
    }
}