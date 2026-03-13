using UnityEngine;
using UnityEngine.UI;

public class UIGuide : UIView
{
    [Header("References")]
    [SerializeField] private Button _backButton;
    void OnEnable()
    {
        _backButton.onClick.AddListener(HandleClickBack);
    }
    void OnDisable()
    {
        _backButton.onClick.RemoveListener(HandleClickBack);
    }
    private void HandleClickBack()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        Hide();
    }
}
