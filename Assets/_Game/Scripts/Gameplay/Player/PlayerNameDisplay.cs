using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;

public class PlayerNameDisplay : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _nameContainerTransform;
    [SerializeField] private TextMeshProUGUI _nameTMP;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Image _voiceDetectedImg;
    [SerializeField] private VoiceActivityEventChannelSO _voiceActivityEventChannel;
    private Camera _camera;
    private string _id;

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
        _voiceDetectedImg.gameObject.SetActive(false);
        _voiceActivityEventChannel.OnEventRaised += HandleVoiceActivityUpdated;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _playerName.OnValueChanged -= HandleChangeName;
        _voiceActivityEventChannel.OnEventRaised -= HandleVoiceActivityUpdated;
    }

    // Update handler to use FixedString128Bytes
    private void HandleChangeName(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        _nameTMP.text = newValue.ToString();
    }

    // Update SetPlayerName to use FixedString128Bytes
    public void SetPlayerName(string name, string authId)
    {
        _playerName.Value = new FixedString128Bytes(name);
        _id = authId;
    }

    void LateUpdate()
    {
        if (_camera == null) return;
        _nameContainerTransform.LookAt(_camera.transform);
    }
    private void HandleVoiceActivityUpdated(string authId, bool isActive)
    {
        if (authId != _id) return;
        _voiceDetectedImg.gameObject.SetActive(isActive);
    }
}