using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIPointerEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool _isHoveredd = false;
    public bool IsHovered => _isHoveredd;
    public event Action<bool> OnPointerHover;
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHoveredd = true;
        OnPointerHover?.Invoke(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHoveredd = false;
        OnPointerHover?.Invoke(false);
    }
}
