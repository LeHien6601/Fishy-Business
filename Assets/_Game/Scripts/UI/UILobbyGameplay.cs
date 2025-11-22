using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILobbyGameplay : UIView
{
    [Header("References")]
    [SerializeField] private Button _continueBtn;
    [SerializeField] private Button _lobbyInfoBtn;
    [SerializeField] private Button _settingsBtn;
    [SerializeField] private Button _leaveLobbyBtn;
    [SerializeField] private Button _exitToDesktopBtn;
    void OnEnable()
    {
        _continueBtn.onClick.AddListener(HandleClickContinue);
        _lobbyInfoBtn.onClick.AddListener(HandleClickLobbyInfo);
        _settingsBtn.onClick.AddListener(HandleClickSettings);
        _leaveLobbyBtn.onClick.AddListener(HandleClickLeaveLobby);
        _exitToDesktopBtn.onClick.AddListener(HandleClickExitToDesktop);
    }
    void OnDisable()
    {
        _continueBtn.onClick.RemoveListener(HandleClickContinue);
        _lobbyInfoBtn.onClick.RemoveListener(HandleClickLobbyInfo);
        _settingsBtn.onClick.RemoveListener(HandleClickSettings);
        _leaveLobbyBtn.onClick.RemoveListener(HandleClickLeaveLobby);
        _exitToDesktopBtn.onClick.RemoveListener(HandleClickExitToDesktop);
    }
    public override void Show()
    {
        base.Show();
        UIManager.Instance.ShowUI(EUIState.Footer);
    }
    public override void HideWithParams(object param)
    {
        base.HideWithParams(param);
        if ((bool)param)
        {
            UIManager.Instance.HideUI(EUIState.Footer);
        }
    }
    private void HandleClickContinue()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        Debug.Log("Continue button clicked");
        HideWithParams(true);
        EventSystem.current.SetSelectedGameObject(null);
    }
    private void HandleClickLobbyInfo()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        Hide();
        UIManager.Instance.ShowUI(EUIState.LobbyInfo);
        EventSystem.current.SetSelectedGameObject(null);
    }
    private void HandleClickSettings()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        // Settings button logic here
        Debug.Log("Settings button clicked");
        EventSystem.current.SetSelectedGameObject(null);
    }
    private async void HandleClickLeaveLobby()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        await LobbyManager.Instance.LeaveLobbyAsync();
        Hide();
        UIManager.Instance.ShowUI(EUIState.MainMenu);
        GameManager.Instance.DespawnPlayerRpc(NetworkManager.Singleton.LocalClientId);
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        GameManager.Instance.HandleLoadComplete(NetworkManager.Singleton.LocalClientId, "Lobby", LoadSceneMode.Single);
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void HandleClickExitToDesktop()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        EventSystem.current.SetSelectedGameObject(null);
        Application.Quit();
    }
}
