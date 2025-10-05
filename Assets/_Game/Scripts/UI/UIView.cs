using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UIView : MonoBehaviour
{
    private Canvas _canvas;
    void Awake()
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();
    }
    public void SetSortingOrder(int order)
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = order;
    }
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
    public virtual void ShowWithParams(object param)
    {
        gameObject.SetActive(true);
    }
    public virtual void HideWithParams(object param)
    {
        gameObject.SetActive(false);
    }
}
