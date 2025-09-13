using UnityEngine;
using UnityEngine.UI;

public class UICustomGame : UIView
{
    [Header("References")]
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _createBtn;
    [SerializeField] private Button _joinBtn;

    private void Awake()
    {
        _backBtn.onClick.AddListener(Back);
        _createBtn.onClick.AddListener(Create);
        _joinBtn.onClick.AddListener(Join);
    }
    private void Back()
    {
        UIManager.Instance.ShowUI(EUIState.MainMenu);
        UIManager.Instance.HideUI(EUIState.CustomGame);
    }
    private void Create()
    {

    } 

    private void Join()
    {

    }
}
