using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Vivox;
using HHDCore;
public class VivoxManager : SingletonMono<VivoxManager>
{
    private string currentChannel;

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
        Debug.Log("JoinChannelAsync");
        string channelName = $"lobby_{LobbyManager.Instance.currentLobby.Id}";
        if (currentChannel == channelName)
            return;
        Debug.Log("try");
        try
        {
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
            currentChannel = channelName;
            Debug.Log($"Joined Vivox channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task LeaveChannelAsync()
    {
        if (string.IsNullOrEmpty(currentChannel))
            return;

        try
        {
            await VivoxService.Instance.LeaveChannelAsync(currentChannel);
            Debug.Log("Left Vivox channel");
            currentChannel = null;
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
}