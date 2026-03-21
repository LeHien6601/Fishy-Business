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
    [SerializeField] private Image _nameBackgroundImage;
    [SerializeField] private VoiceActivityEventChannelSO _voiceActivityEventChannel;
    private Camera _camera;
    private NetworkVariable<FixedString512Bytes> _playerId = new(new FixedString512Bytes(""), 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    
    private NetworkVariable<FixedString128Bytes> _playerName = new(new FixedString128Bytes(""), 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _playerName.OnValueChanged += HandleChangeName;
        _playerId.OnValueChanged += HandleChangeId;
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
        _playerId.OnValueChanged -= HandleChangeId;
        _voiceActivityEventChannel.OnEventRaised -= HandleVoiceActivityUpdated;
    }

    private void HandleChangeName(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        _nameTMP.text = newValue.ToString();
    }
    private void HandleChangeId(FixedString512Bytes previousValue, FixedString512Bytes newValue)
    {
        _nameBackgroundImage.color = GameConfig.Instance.GetColor(LobbyManager.Instance.GetPlayerIndex(newValue.ToString()));
    }

    public void SetPlayerName(string name, string authId)
    {
        _playerName.Value = new FixedString128Bytes(name);
        if (authId == null) return;
        _playerId.Value = new FixedString512Bytes(authId);
        _nameBackgroundImage.color = GameConfig.Instance.GetColor(LobbyManager.Instance.GetPlayerIndex(authId));
    }

    void LateUpdate()
    {
        if (_camera == null) return;
        _nameContainerTransform.LookAt(_camera.transform);
    }
    private void HandleVoiceActivityUpdated(string authId, bool isActive)
    {

        if (authId != _playerId.Value) return;
        _voiceDetectedImg.gameObject.SetActive(isActive);
    }
}