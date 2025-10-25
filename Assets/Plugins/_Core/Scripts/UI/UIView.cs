using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UIView : MonoBehaviour
{
    #region Properties
    [Header("Animations")]
    [SerializeField] private UIAnimation _showAnimation;
    [SerializeField] private UIAnimation _hideAnimation;
    private bool _isShowing = false;
    private bool _hasDoneShowAnimation = false;
    private bool _hasDoneHideAnimation = true;
    private Canvas _canvas;
    #endregion
    void Awake()
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();
    }

    #region Getters/Setters
    public bool IsShowing => _isShowing;
    public void SetSortingOrder(int order)
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = order;
    }
    #endregion
    
    #region Behaviors
    public virtual async void Show()
    {
        if (_isShowing) return;
        while (!_hasDoneHideAnimation)
        {
            await Task.Yield();
        }
        _isShowing = true;
        _hasDoneShowAnimation = false;
        gameObject.SetActive(true);
        if (_showAnimation) await _showAnimation.PlayAnimation();
        _hasDoneShowAnimation = true;
    }
    public virtual async void Hide()
    {
        if (!_isShowing) return;
        while (!_hasDoneShowAnimation)
        {
            await Task.Yield();
        }
        _isShowing = false;
        _hasDoneHideAnimation = false;
        if (_hideAnimation)
        {
            await _hideAnimation.PlayAnimation();
        }
        gameObject.SetActive(false);
        _hasDoneHideAnimation = true;
    }
    public virtual async void ShowWithParams(object param)
    {
        if (_isShowing) return;
        while (!_hasDoneHideAnimation)
        {
            await Task.Yield();
        }
        _isShowing = true;
        _hasDoneShowAnimation = false;
        gameObject.SetActive(true);
        if (_showAnimation) await _showAnimation.PlayAnimation();
        _hasDoneShowAnimation = true;
    }
    public virtual async void HideWithParams(object param)
    {
        if (!_isShowing) return;
        while (!_hasDoneShowAnimation)
        {
            await Task.Yield();
        }
        _isShowing = false;
        _hasDoneHideAnimation = false;
        if (_hideAnimation) await _hideAnimation.PlayAnimation();
        gameObject.SetActive(false);
        _hasDoneHideAnimation = true;
    }
    #endregion
}
