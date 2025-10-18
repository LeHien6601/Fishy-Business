using UnityEngine;
using UnityEngine.UI;

public class UIMainMenu : UIView
{
    [Header("References")]
    [SerializeField] private Button _quickMatchBtn;
    [SerializeField] private Button _customGameBtn;
    [SerializeField] private Button _settingsBtn;
    [SerializeField] private Button _exitBtn;

    private void Awake()
    {
        _quickMatchBtn.onClick.AddListener(QuickMatch);
        _customGameBtn.onClick.AddListener(CustomGame);
        _settingsBtn.onClick.AddListener(Settings);
        _exitBtn.onClick.AddListener(Exit);
    }

    public override void Show()
    {
        base.Show();
        UIManager.Instance.ShowUI(EUIState.Footer);
    }

    private async void QuickMatch()
    {
        await LobbyManager.Instance.QuickJoinAsync(GameManager.Instance.PlayerName, GameManager.Instance.PlayerIconID);
        UIManager.Instance.HideUI(EUIState.MainMenu);
        UIManager.Instance.HideUI(EUIState.Footer);
    }
    private void CustomGame()
    {
        UIManager.Instance.ShowUI(EUIState.CustomGame);
        UIManager.Instance.HideUI(EUIState.MainMenu);
    }
    private void Settings()
    {

    }
    private void Exit()
    {
        Application.Quit();
    }
}
