using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMainMenu : UIView
{
    [Header("References")]
    [SerializeField] private Button _quickMatchBtn;
    [SerializeField] private Button _customGameBtn;
    [SerializeField] private Button _settingsBtn;
    [SerializeField] private Button _exitBtn;
    [SerializeField] private TextMeshProUGUI _messageTMP;

    [Header("Properties")]
    [SerializeField] private string _quickMatchFailureMessage = "No rooms found for Quick Match. You can create a new room or try again later.";
    private void Awake()
    {
        _quickMatchBtn.onClick.AddListener(QuickMatch);
        _customGameBtn.onClick.AddListener(CustomGame);
        _settingsBtn.onClick.AddListener(Settings);
        _exitBtn.onClick.AddListener(Exit);
        _messageTMP.gameObject.SetActive(false);
    }

    public override void Show()
    {
        base.Show();
        UIManager.Instance.ShowUI(EUIState.Footer);
    }

    private async void QuickMatch()
    {
        try
        {
            Task task = LobbyManager.Instance.QuickJoinAsync(PlayerInfoManager.Instance.PlayerName, PlayerInfoManager.Instance.PlayerIconId);
            await task;
            if (task.Status == TaskStatus.Faulted)
            {
                Debug.LogError("Quick Join task faulted.");
                return;
            }
            else
            {
                UIManager.Instance.HideUI(EUIState.MainMenu);
                UIManager.Instance.HideUI(EUIState.Footer);
                await Task.Delay(500);
                UIManager.Instance.ShowUI(EUIState.Loading);
                while (!task.IsCompleted)
                {
                    await Task.Yield();
                }
                await Task.Delay(500);
                UIManager.Instance.HideUI(EUIState.Loading);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Quick Join failed: " + ex.Message);
            ShowMessage(_quickMatchFailureMessage, 5f);
            return;
        }
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
    private async void ShowMessage(string message, float duration = 2f)
    {
        _messageTMP.text = message;
        _messageTMP.gameObject.SetActive(true);
        _messageTMP.rectTransform.localScale = Vector3.zero;
        _messageTMP.rectTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        await Task.Delay((int)((duration - 0.5f) * 1000));
        _messageTMP.rectTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
        await Task.Delay(500);
        _messageTMP.gameObject.SetActive(false);
    }
}
