using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UIView : MonoBehaviour
{
    [Header("Animations")]
    [SerializeField] private UIAnimation _showAnimation;
    [SerializeField] private UIAnimation _hideAnimation;
    private bool _isShowing = false;
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
        if (_isShowing) return;
        _isShowing = true;
        gameObject.SetActive(true);
        if (_showAnimation) _showAnimation.PlayAnimation();
    }
    public virtual async void Hide()
    {
        if (!_isShowing) return;
        _isShowing = false;
        if (_hideAnimation)
        {
            _hideAnimation.PlayAnimation();
            await Task.Delay((int)(1000 * _hideAnimation.GetOverallDuration()));
        }
        gameObject.SetActive(false);
    }
    public virtual void ShowWithParams(object param)
    {
        if (_isShowing) return;
        _isShowing = true;
        gameObject.SetActive(true);
        if (_showAnimation) _showAnimation.PlayAnimation();
    }
    public virtual async void HideWithParams(object param)
    {
        if (!_isShowing) return;
        _isShowing = false;
        if (_hideAnimation)
        {
            _hideAnimation.PlayAnimation();
            await Task.Delay((int)(1000 * _hideAnimation.GetOverallDuration()));
        }
        gameObject.SetActive(false);
    }
}
