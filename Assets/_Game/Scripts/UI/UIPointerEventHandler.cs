using UnityEngine;
using UnityEngine.EventSystems;

public class UIPointerEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool _isHovered = false;
    public bool IsHovered => _isHovered;
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
    }
}
