using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIAnimation))]
public class UIAnimationEditor : Editor
{
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Rotate", "Move", "Scale", "Fade" };

    private AnimationPresets presetsAsset;
    private int selectedPresetIndex = -1;
    private string newPresetName = "";

private void OnEnable()
    {
        presetsAsset = LoadOrCreatePresetsAsset();
    }

    private AnimationPresets LoadOrCreatePresetsAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationPresets");
        if (guids.Length == 0)
        {
            AnimationPresets newAsset = CreateInstance<AnimationPresets>();
            AssetDatabase.CreateAsset(newAsset, "Assets/Resources/AnimationPresets.asset");
            AssetDatabase.SaveAssets();
            return newAsset;
        }
        else
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AnimationPresets>(path);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        UIAnimation targetAnim = target as UIAnimation;

        // Preset Management Section
        EditorGUILayout.LabelField("Preset Management", EditorStyles.boldLabel);

        string[] presetNames = presetsAsset.Presets.Select(p => p.Name).ToArray();
        selectedPresetIndex = EditorGUILayout.Popup("Select Preset", selectedPresetIndex, presetNames.Length > 0 ? presetNames : new string[] { "No Presets Available" });

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply Preset") && selectedPresetIndex >= 0 && selectedPresetIndex < presetsAsset.Presets.Count)
        {
            ApplyPreset();
        }
        if (GUILayout.Button("Delete Preset") && selectedPresetIndex >= 0 && selectedPresetIndex < presetsAsset.Presets.Count)
        {
            DeletePreset();
        }
        EditorGUILayout.EndHorizontal();

        newPresetName = EditorGUILayout.TextField("New Preset Name", newPresetName);
        if (GUILayout.Button("Create Preset"))
        {
            CreatePreset(targetAnim);
        }

        EditorGUILayout.Space(10f);
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Type"), new GUIContent("Name"));
        // Draw tab toolbar
        EditorGUILayout.Space();
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(25f));
        EditorGUILayout.Space();

        // Draw fields for the selected tab
        switch (selectedTab)
        {
            case 0: // Rotate
                // EditorGUILayout.LabelField("Rotate Animation", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RotateEnabled"), new GUIContent("Enabled"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RotateDuration"), new GUIContent("Duration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RotateDelay"), new GUIContent("Delay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RotateEaseType"), new GUIContent("Ease Type"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RotateStartValue"), new GUIContent("Start Value"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RotateEndValue"), new GUIContent("End Value"));
                break;
            case 1: // Move
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MoveEnabled"), new GUIContent("Enabled"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MoveDuration"), new GUIContent("Duration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MoveDelay"), new GUIContent("Delay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MoveEaseType"), new GUIContent("Ease Type"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MoveStartValue"), new GUIContent("Start Value"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MoveEndValue"), new GUIContent("End Value"));
                break;
            case 2: // Scale
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ScaleEnabled"), new GUIContent("Enabled"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ScaleDuration"), new GUIContent("Duration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ScaleDelay"), new GUIContent("Delay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ScaleEaseType"), new GUIContent("Ease Type"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ScaleStartValue"), new GUIContent("Start Value"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ScaleEndValue"), new GUIContent("End Value"));
                break;
            case 3: // Fade
                EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeEnabled"), new GUIContent("Enabled"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeDuration"), new GUIContent("Duration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeDelay"), new GUIContent("Delay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeEaseType"), new GUIContent("Ease Type"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeStartValue"), new GUIContent("Start Value"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeEndValue"), new GUIContent("End Value"));
                break;
        }

        // Apply changes to serialized properties
        serializedObject.ApplyModifiedProperties();

        // Play Animation button
        EditorGUILayout.Space(10f);
        if (GUILayout.Button("Play Animation", GUILayout.Height(30f)))
        {
            ((UIAnimation)target).PlayAnimation();
        }
    }

    private void ApplyPreset()
    {
        AnimationPreset preset = presetsAsset.Presets[selectedPresetIndex];

        // Rotate
        serializedObject.FindProperty("RotateEnabled").boolValue = preset.RotateEnabled;
        serializedObject.FindProperty("RotateDuration").floatValue = preset.RotateDuration;
        serializedObject.FindProperty("RotateDelay").floatValue = preset.RotateDelay;
        serializedObject.FindProperty("RotateEaseType").enumValueIndex = (int)preset.RotateEaseType;
        serializedObject.FindProperty("RotateStartValue").vector3Value = preset.RotateStartValue;
        serializedObject.FindProperty("RotateEndValue").vector3Value = preset.RotateEndValue;

        // Move
        serializedObject.FindProperty("MoveEnabled").boolValue = preset.MoveEnabled;
        serializedObject.FindProperty("MoveDuration").floatValue = preset.MoveDuration;
        serializedObject.FindProperty("MoveDelay").floatValue = preset.MoveDelay;
        serializedObject.FindProperty("MoveEaseType").enumValueIndex = (int)preset.MoveEaseType;
        serializedObject.FindProperty("MoveStartValue").vector2Value = preset.MoveStartValue;
        serializedObject.FindProperty("MoveEndValue").vector2Value = preset.MoveEndValue;

        // Scale
        serializedObject.FindProperty("ScaleEnabled").boolValue = preset.ScaleEnabled;
        serializedObject.FindProperty("ScaleDuration").floatValue = preset.ScaleDuration;
        serializedObject.FindProperty("ScaleDelay").floatValue = preset.ScaleDelay;
        serializedObject.FindProperty("ScaleEaseType").enumValueIndex = (int)preset.ScaleEaseType;
        serializedObject.FindProperty("ScaleStartValue").vector2Value = preset.ScaleStartValue;
        serializedObject.FindProperty("ScaleEndValue").vector2Value = preset.ScaleEndValue;

        // Fade
        serializedObject.FindProperty("FadeEnabled").boolValue = preset.FadeEnabled;
        serializedObject.FindProperty("FadeDuration").floatValue = preset.FadeDuration;
        serializedObject.FindProperty("FadeDelay").floatValue = preset.FadeDelay;
        serializedObject.FindProperty("FadeEaseType").enumValueIndex = (int)preset.FadeEaseType;
        serializedObject.FindProperty("FadeStartValue").floatValue = preset.FadeStartValue;
        serializedObject.FindProperty("FadeEndValue").floatValue = preset.FadeEndValue;

        serializedObject.ApplyModifiedProperties();
    }

    private void CreatePreset(UIAnimation targetAnim)
    {
        if (string.IsNullOrEmpty(newPresetName))
        {
            Debug.LogWarning("Preset name cannot be empty.");
            return;
        }
        if (presetsAsset.Presets.Any(p => p.Name == newPresetName))
        {
            Debug.LogWarning("Preset name already exists.");
            return;
        }

        AnimationPreset newPreset = new()
        {
            Name = newPresetName,

            // Rotate
            RotateEnabled = targetAnim.RotateEnabled,
            RotateDuration = targetAnim.RotateDuration,
            RotateDelay = targetAnim.RotateDelay,
            RotateEaseType = targetAnim.RotateEaseType,
            RotateStartValue = targetAnim.RotateStartValue,
            RotateEndValue = targetAnim.RotateEndValue,

            // Move
            MoveEnabled = targetAnim.MoveEnabled,
            MoveDuration = targetAnim.MoveDuration,
            MoveDelay = targetAnim.MoveDelay,
            MoveEaseType = targetAnim.MoveEaseType,
            MoveStartValue = targetAnim.MoveStartValue,
            MoveEndValue = targetAnim.MoveEndValue,

            // Scale
            ScaleEnabled = targetAnim.ScaleEnabled,
            ScaleDuration = targetAnim.ScaleDuration,
            ScaleDelay = targetAnim.ScaleDelay,
            ScaleEaseType = targetAnim.ScaleEaseType,
            ScaleStartValue = targetAnim.ScaleStartValue,
            ScaleEndValue = targetAnim.ScaleEndValue,

            // Fade
            FadeEnabled = targetAnim.FadeEnabled,
            FadeDuration = targetAnim.FadeDuration,
            FadeDelay = targetAnim.FadeDelay,
            FadeEaseType = targetAnim.FadeEaseType,
            FadeStartValue = targetAnim.FadeStartValue,
            FadeEndValue = targetAnim.FadeEndValue
        };

        presetsAsset.Presets.Add(newPreset);
        EditorUtility.SetDirty(presetsAsset);
        AssetDatabase.SaveAssets();
        newPresetName = "";
        selectedPresetIndex = presetsAsset.Presets.Count - 1;
    }

    private void DeletePreset()
    {
        presetsAsset.Presets.RemoveAt(selectedPresetIndex);
        EditorUtility.SetDirty(presetsAsset);
        AssetDatabase.SaveAssets();
        selectedPresetIndex = -1;
    }
}