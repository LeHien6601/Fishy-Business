using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Vivox;
using HHDCore;
public class VivoxManager : SingletonMono<VivoxManager>
{
    private string _currentChannel;

    private async void Start()
    {
        await InitializeAsync();
        LobbyManager.Instance.OnJoinedLobby += HandleJoinedLobby;
        LobbyManager.Instance.OnLeftLobby += HandleLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby += HandleKickedFromLobby;
    }
        private void OnDestroy()
    {
        LobbyManager.Instance.OnJoinedLobby -= HandleJoinedLobby;
        LobbyManager.Instance.OnLeftLobby -= HandleLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby -= HandleKickedFromLobby;
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

}