using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using HHDCore;

[System.Serializable]
public class AnimationPreset
{
    public string Name;

    // Rotate
    public bool RotateEnabled;
    public float RotateDuration;
    public float RotateDelay;
    public Ease RotateEaseType;
    public Vector3 RotateStartValue;
    public Vector3 RotateEndValue;

    // Move
    public bool MoveEnabled;
    public Vector2 MoveStartValue;
    public Vector2 MoveEndValue;
    public float MoveDuration;
    public float MoveDelay;
    public Ease MoveEaseType;

    // Scale
    public bool ScaleEnabled;
    public float ScaleDuration;
    public float ScaleDelay;
    public Ease ScaleEaseType;
    public Vector3 ScaleStartValue;
    public Vector3 ScaleEndValue;

    // Fade
    public bool FadeEnabled;
    public float FadeDuration;
    public float FadeDelay;
    public Ease FadeEaseType;
    public float FadeStartValue;
    public float FadeEndValue;

}

[CreateAssetMenu(fileName = "AnimationPresets", menuName = "Animation/AnimationPresets")]
public class AnimationPresets : SingletonScriptableObject<AnimationPresets>
{
    public List<AnimationPreset> Presets = new();
    public List<AnimationPreset> ButtonPresets = new();
}