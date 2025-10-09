using UnityEngine;
using DG.Tweening;
using UnityEditor;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class UIAnimation : MonoBehaviour
{
    #region Properties
    public bool MoveEnabled = false;
    public float MoveDuration = 1f;
    public float MoveDelay = 0f;
    public Ease MoveEaseType = Ease.Linear;
    public Vector2 MoveStartValue = Vector2.zero;
    public Vector2 MoveEndValue = Vector2.zero;

    public bool RotateEnabled = false;
    public float RotateDuration = 1f;
    public float RotateDelay = 0f;
    public Ease RotateEaseType = Ease.Linear;
    public Vector3 RotateStartValue = Vector2.zero;
    public Vector3 RotateEndValue = Vector2.zero;

    public bool ScaleEnabled = false;
    public float ScaleDuration = 1f;
    public float ScaleDelay = 0f;
    public Ease ScaleEaseType = Ease.Linear;
    public Vector2 ScaleStartValue = Vector2.zero;
    public Vector2 ScaleEndValue = Vector2.one;

    public bool FadeEnabled = false;
    public float FadeDuration = 1f;
    public float FadeDelay = 0f;
    public Ease FadeEaseType = Ease.Linear;
    public float FadeStartValue = 0;
    public float FadeEndValue = 1;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private double _lastEditorTime;

    #endregion

    #region Editor
#if UNITY_EDITOR
    void OnEnable()
    {
        EditorApplication.update += EditorUpdate;
        _lastEditorTime = EditorApplication.timeSinceStartup;
    }
    void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
    }
#endif
    private void EditorUpdate()
    {
        if (Application.isPlaying) return;
        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentTime - _lastEditorTime);
        _lastEditorTime = currentTime;
        DOTween.ManualUpdate(deltaTime, deltaTime);
    }

    #endregion

    #region Behaviors
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    [ContextMenu("Play Animation")]
    public void PlayAnimation()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
        }
#endif
        _rectTransform ??= GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("UIAnimation requires a RectTransform component.");
            return;
        }

        _canvasGroup ??= GetComponent<CanvasGroup>();
        
        // Kill any existing tweens
        DOTween.Kill(gameObject);

        // Set initial values
        if (MoveEnabled)
        {
            float width = _rectTransform.rect.width, height = _rectTransform.rect.height;
            _rectTransform.anchoredPosition = new Vector2(width * MoveStartValue.x, height * MoveStartValue.y);
        }
        if (RotateEnabled) _rectTransform.eulerAngles = RotateStartValue;
        if (ScaleEnabled) _rectTransform.localScale = ScaleStartValue;
        if (FadeEnabled)
        {
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = FadeStartValue;
        }

        // Start animations
        if (MoveEnabled) AnimateMove();
        if (RotateEnabled) AnimateRotate();
        if (ScaleEnabled) AnimateScale();
        if (FadeEnabled) AnimateFade();
    }

    private void AnimateMove()
    {
        float width = _rectTransform.rect.width, height = _rectTransform.rect.height;
        Vector2 endPosition = new(width * MoveEndValue.x, height * MoveEndValue.y);
        _rectTransform.DOAnchorPos(endPosition, MoveDuration)
            .SetDelay(MoveDelay)
            .SetEase(MoveEaseType)
            .SetUpdate(Application.isPlaying ? UpdateType.Normal : UpdateType.Manual, !Application.isPlaying);
    }

    private void AnimateRotate()
    {
        _rectTransform.DORotate(RotateEndValue, RotateDuration)
            .SetDelay(RotateDelay)
            .SetEase(RotateEaseType)
            .SetUpdate(Application.isPlaying ? UpdateType.Normal : UpdateType.Manual, !Application.isPlaying);
    }

    private void AnimateScale()
    {
        _rectTransform.DOScale(ScaleEndValue, ScaleDuration)
            .SetDelay(ScaleDelay)
            .SetEase(ScaleEaseType)
            .SetUpdate(Application.isPlaying ? UpdateType.Normal : UpdateType.Manual, !Application.isPlaying);
    }

    private void AnimateFade()
    {
        _canvasGroup.DOFade(FadeEndValue, FadeDuration)
            .SetDelay(FadeDelay)
            .SetEase(FadeEaseType)
            .SetUpdate(Application.isPlaying ? UpdateType.Normal : UpdateType.Manual, !Application.isPlaying);
    }

    #endregion
}
