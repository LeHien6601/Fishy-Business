using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISettings : UIView
{
    [Header("References")]
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _saveButton;
    [SerializeField] private TextMeshProUGUI _noteTMP;

    void OnEnable()
    {
        UpdateUI();
        _backButton.onClick.AddListener(HandleClickBack);
        _saveButton.onClick.AddListener(HandleClickSave);
        _musicVolumeSlider.onValueChanged.AddListener((a) => HandleChangeSlider(a));
        _sfxVolumeSlider.onValueChanged.AddListener((a) => HandleChangeSlider(a));
    }
    void OnDisable()
    {
        _backButton.onClick.RemoveListener(HandleClickBack);
        _saveButton.onClick.RemoveListener(HandleClickSave);
        _musicVolumeSlider.onValueChanged.RemoveAllListeners();
        _sfxVolumeSlider.onValueChanged.RemoveAllListeners();
    }
    private void UpdateUI()
    {
        _musicVolumeSlider.value = SoundManager.GetMusicVolume();
        _sfxVolumeSlider.value = SoundManager.GetSFXVolume();
        _noteTMP.gameObject.SetActive(false);
    }
    private void HandleChangeSlider(float value)
    {
        SoundManager.Play2D(SoundType.ButtonClick);
    }
    private void HandleClickBack()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        Hide();
    }
    private void HandleClickSave()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        SoundManager.SetMusicVolume(_musicVolumeSlider.value);
        SoundManager.SetSFXVolume(_sfxVolumeSlider.value);
        ShowNote();
    }
    private async void ShowNote()
    {
        _noteTMP.gameObject.SetActive(true);
        _noteTMP.rectTransform.localScale = Vector3.zero;
        _noteTMP.rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        await Task.Delay(2000);
        _noteTMP.rectTransform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            _noteTMP.gameObject.SetActive(false);
        });
    }
}
