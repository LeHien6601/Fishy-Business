using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Vivox;
using HHDCore;
using UnityEngine.InputSystem;
public class VivoxManager : SingletonMono<VivoxManager>
{
    [SerializeField] private VoiceActivityEventChannelSO _voiceActivityEventChannel;
    private string _currentChannel;
    private bool _isPushToTalkEnabled = true;
    public bool IsPushToTalkEnabled
    {
        get => _isPushToTalkEnabled;
        set
        {
            _isPushToTalkEnabled = value;
            PlayerPrefs.SetInt(PUSH_TO_TALK_PREF_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
            if (!_isPushToTalkEnabled)
                SetSelfMute(false); // Unmute if push-to-talk is disabled
            else
                SetSelfMute(true);  // Mute if push-to-talk is enabled
        }
    }
    private bool _isGlobalSelfMute = false;
    public bool IsGlobalSelfMute
    {
        get => _isGlobalSelfMute;
        set
        {
            _isGlobalSelfMute = value;
            PlayerPrefs.SetInt(GLOBAL_SELF_MUTE_PREF_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
    private const string PUSH_TO_TALK_PREF_KEY = "Vivox_PushToTalkEnabled";
    private const string GLOBAL_SELF_MUTE_PREF_KEY = "Vivox_GlobalSelfMuteEnabled";

    private async void Start()
    {
        await InitializeAsync();
        VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
        LobbyManager.Instance.OnJoinedLobby += HandleJoinedLobby;
        LobbyManager.Instance.OnLeftLobby += HandleLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby += HandleKickedFromLobby;
    }
    private void OnDestroy()
    {
        VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAdded;
        LobbyManager.Instance.OnJoinedLobby -= HandleJoinedLobby;
        LobbyManager.Instance.OnLeftLobby -= HandleLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby -= HandleKickedFromLobby;
    }

    private void OnParticipantAdded(VivoxParticipant participant)
    {
        participant.ParticipantSpeechDetected += () => OnSpeechDetected(participant);
    }

    private void OnSpeechDetected(VivoxParticipant participant)
    {
        bool isSpeaking = participant.SpeechDetected;
        string playerId = participant.PlayerId;

        if (_voiceActivityEventChannel != null)
        {
            _voiceActivityEventChannel.RaiseEvent(playerId, isSpeaking);
        }
    }

    void Update()
    {
        if (!_isPushToTalkEnabled) return;
        // Push-to-talk: Press 'V' to talk, release to mute
        if (Keyboard.current?.vKey.wasPressedThisFrame == true)
        {
            SetSelfMute(false);
        }
        else if (Keyboard.current?.vKey.wasReleasedThisFrame == true)
        {
            SetSelfMute(true);
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            while (!LobbyManager.Instance.InitServices)
            {
                await Task.Yield();
            }
            await VivoxService.Instance.InitializeAsync();
            IsPushToTalkEnabled = PlayerPrefs.GetInt(PUSH_TO_TALK_PREF_KEY, 1) == 1;
            IsGlobalSelfMute = PlayerPrefs.GetInt(GLOBAL_SELF_MUTE_PREF_KEY, 0) == 1;
            // Optional: Fine-tune how sensitive the "Talking" detection is
            // hangover: ms to wait after silence (default 2000ms)
            // sensitivity: 0 is most sensitive, 100 is least (default 43)
            await VivoxService.Instance.SetVoiceActivityDetectionPropertiesAsync(hangover: 500, noiseFloor: 576, sensitivity: 43);
            Debug.Log("Vivox Initialized");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

#region Event Handler
    private async void HandleJoinedLobby()
    {
        await LoginAsync();
        await JoinChannelAsync();
    }

    private async void HandleLeftLobby()
    {
        await LeaveChannelAsync();
    }

    private async void HandleKickedFromLobby(LobbyManager.KickedFromLobbyEventArgs args)
    {
        await LeaveChannelAsync();
    }

#endregion
    private async Task LoginAsync()
    {
        if (VivoxService.Instance.IsLoggedIn)
            return;

        try
        {
            await VivoxService.Instance.LoginAsync();
            Debug.Log("Vivox Logged In");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task JoinChannelAsync()
    {
        if (LobbyManager.Instance.currentLobby == null) return;
        string channelName = $"lobby_{LobbyManager.Instance.currentLobby.Id}";
        if (_currentChannel == channelName)
            return;
        try
        {
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
            _currentChannel = channelName;
            Debug.Log($"Joined Vivox channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task LeaveChannelAsync()
    {
        if (string.IsNullOrEmpty(_currentChannel))
            return;

        try
        {
            await VivoxService.Instance.LeaveChannelAsync(_currentChannel);
            Debug.Log("Left Vivox channel");
            _currentChannel = null;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async void OnApplicationQuit()
    {
        if (VivoxService.Instance.IsLoggedIn)
        {
            await LeaveChannelAsync();
            await VivoxService.Instance.LogoutAsync();
            Debug.Log("Vivox Logged Out");
        }
    }

    public void UpdatePlayerVoiceChatVolume(string authId, int volume)
    {
        foreach (var channel in VivoxService.Instance.ActiveChannels)
        {
            if (channel.Key != _currentChannel) continue;
            foreach (var player in channel.Value)
            {
                if (player.PlayerId != authId) continue;
                player.SetLocalVolume(volume);  
                break;            
            }
        }
    }

    public int GetVoiceChatVolumnByAuthId(string authId)
    {
        foreach (var channel in VivoxService.Instance.ActiveChannels)
        {
            if (channel.Key != _currentChannel) continue;
            foreach (var player in channel.Value)
            {
                if (player.PlayerId != authId) continue;
                return player.LocalVolume;             
            }
        }
        return 0;
    }

    public void SetSelfMute(bool isMuted)
    {
        try
        {
            // This toggles the local microphone input for the Vivox client
            if (isMuted)
                VivoxService.Instance.MuteInputDevice();
            else if (!IsGlobalSelfMute)
                VivoxService.Instance.UnmuteInputDevice();

            Debug.Log($"Vivox: Self mute set to {isMuted}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Vivox: Failed to set self mute: {e.Message}");
        }
    }
    
}