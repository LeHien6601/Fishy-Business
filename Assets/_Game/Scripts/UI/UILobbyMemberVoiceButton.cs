using System.Collections.Generic;
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
    private bool _isShowingSlider = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ToggleShowingSliderContainer();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        foreach (var handler in _pointerEventHandlers)
        {
            if (handler.IsHovered) return;
        }
        ToggleShowingSliderContainer();
    }

    private void ToggleShowingSliderContainer()
    {
        _isShowingSlider = !_isShowingSlider;
        _sliderContainerBackgroundImg.enabled = _isShowingSlider;
        _sliderContainer.gameObject.SetActive(_isShowingSlider);
    }
}