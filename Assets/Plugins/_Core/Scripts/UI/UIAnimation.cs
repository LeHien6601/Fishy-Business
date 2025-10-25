using UnityEngine;
using DG.Tweening;
using UnityEditor;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.UIElements;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class UIAnimation : MonoBehaviour
{
    #region Properties
    public UIAnimationType Type;
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
    public async Task PlayAnimation()
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
        else _canvasGroup.alpha = 1;
        await Task.Delay((int)(1000 * GetOverallDuration()));
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            return;
        }
        await Task.Delay(500);
        _rectTransform.sizeDelta = Vector2.zero;
        _canvasGroup.alpha = 1;
        _rectTransform.localScale = Vector2.one;
        if (Type == UIAnimationType.Hide || Type == UIAnimationType.Show)
        {
            _rectTransform.localPosition = Vector3.zero;
        }
#endif
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

    #region Getters
    public float GetOverallDuration()
    {
        float duration = 0;
        if (MoveEnabled) duration = Mathf.Max(duration, MoveDuration + MoveDelay);
        if (RotateEnabled) duration = Mathf.Max(duration, RotateDuration + RotateDelay);
        if (ScaleEnabled) duration = Mathf.Max(duration, ScaleDuration + ScaleDelay);
        if (FadeEnabled) duration = Mathf.Max(duration, FadeDuration + FadeDelay);
        return duration;
    }
    #endregion

    #region Setters
    public void SetAnimation(UIAnimationType type, int index)
    {
        List<AnimationPreset> presets = (type == UIAnimationType.Button) ?
            AnimationPresets.Instance.ButtonPresets :
            AnimationPresets.Instance.Presets;
        if (index < 0 || index >= presets.Count) return;
        AnimationPreset preset = presets[index];
        Type = type;
        // Move
        MoveEnabled = preset.MoveEnabled;
        MoveDuration = preset.MoveDuration;
        MoveDelay = preset.MoveDelay;
        MoveEaseType = preset.MoveEaseType;
        MoveStartValue = preset.MoveStartValue;
        MoveEndValue = preset.MoveEndValue;
        // Rotate
        RotateEnabled = preset.RotateEnabled;
        RotateDuration = preset.RotateDuration;
        RotateDelay = preset.RotateDelay;
        RotateEaseType = preset.RotateEaseType;
        RotateStartValue = preset.RotateStartValue;
        RotateEndValue = preset.RotateEndValue;
        // Fade
        FadeEnabled = preset.FadeEnabled;
        FadeDuration = preset.FadeDuration;
        FadeDelay = preset.FadeDelay;
        FadeEaseType = preset.FadeEaseType;
        FadeStartValue = preset.FadeStartValue;
        FadeEndValue = preset.FadeEndValue;
        // Rotate
        ScaleEnabled = preset.ScaleEnabled;
        ScaleDuration = preset.ScaleDuration;
        ScaleDelay = preset.ScaleDelay;
        ScaleEaseType = preset.ScaleEaseType;
        ScaleStartValue = preset.ScaleStartValue;
        ScaleEndValue = preset.ScaleEndValue;
    }
    #endregion

}

public enum UIAnimationType {Show,Hide,Button,Others}