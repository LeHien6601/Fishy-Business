using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class UILobbyMemberVoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sound References")]
    [SerializeField] private Button _soundBTN;
    [SerializeField] private Image _sliderContainerBackgroundImg;
    [SerializeField] private RectTransform _sliderContainer;
    [SerializeField] private Slider _voiceVolumeSlider;
    [SerializeField] private List<UIPointerEventHandler> _pointerEventHandlers = new();
    private bool _isHorveringButton = false;
    public event Action<VoiceChatVolumnChangedEventArgs> OnVoiceChatVolumeChanged;
    public struct VoiceChatVolumnChangedEventArgs
    {
        public int NewVolume;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHorveringButton = true;
        HandleShowingSliderContainer();
    }

    public async void OnPointerExit(PointerEventData eventData)
    {
        _isHorveringButton = false;
        await Task.Delay(100);
        HandleShowingSliderContainer();
    }

    private void HandleShowingSliderContainer()
    {
        bool elementHovering = false;
        foreach (var handler in _pointerEventHandlers)
        {
            if (handler.IsHovered)
            {
                elementHovering = true;
                break;
            }
        }
        bool show = _isHorveringButton || elementHovering;
        _sliderContainerBackgroundImg.enabled = show;
        _sliderContainer.gameObject.SetActive(show);
    }

    void OnEnable()
    {
        _voiceVolumeSlider.onValueChanged.AddListener(HandleSliderValueChanged);
        foreach (var handler in _pointerEventHandlers)
        {
            handler.OnPointerHover += HandlePointerHover;
        }
    }
    void OnDisable()
    {
        _voiceVolumeSlider.onValueChanged.RemoveListener(HandleSliderValueChanged);
        foreach (var handler in _pointerEventHandlers)
        {
            handler.OnPointerHover -= HandlePointerHover;
        }
    }

    public void SetSliderValue(int value)
    {
        _voiceVolumeSlider.value = value;
    }

    private void HandlePointerHover(bool a)
    {
        HandleShowingSliderContainer();
    }

    private async void HandleSliderValueChanged(float newValue)
    {
        int volume = Mathf.CeilToInt(Mathf.Clamp(newValue, -50, 50));
        await Task.Delay(100);
        int sliderValue = Mathf.CeilToInt(Mathf.Clamp(_voiceVolumeSlider.value, -50, 50));
        if (volume == sliderValue)
        {
            OnVoiceChatVolumeChanged?.Invoke(new()
            {
                NewVolume = volume
            });
        }
    }
}