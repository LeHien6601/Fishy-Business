using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIButton))]
public class UIButtonEditor : Editor
{
    private AnimationPresets presetsAsset;

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
        base.OnInspectorGUI();
        // Preset Management Section
        EditorGUILayout.LabelField("Preset Management", EditorStyles.boldLabel);
        List<AnimationPreset> presets = presetsAsset.ButtonPresets;
        string[] presetNames = presets.Select(p => p.Name).ToArray();
        SerializedProperty enterProp = serializedObject.FindProperty("EnterPresetIndex");
        SerializedProperty exitProp = serializedObject.FindProperty("ExitPresetIndex");
        SerializedProperty clickProp = serializedObject.FindProperty("ClickPresetIndex");
        enterProp.intValue = EditorGUILayout.Popup("Enter Preset", enterProp.intValue, presetNames.Length > 0 ? presetNames : new string[] { "No Presets Available" });
        exitProp.intValue = EditorGUILayout.Popup("Exit Preset", exitProp.intValue, presetNames.Length > 0 ? presetNames : new string[] { "No Presets Available" });
        clickProp.intValue = EditorGUILayout.Popup("Click Preset", clickProp.intValue, presetNames.Length > 0 ? presetNames : new string[] { "No Presets Available" });
    
        // Apply changes to serialized properties
        serializedObject.ApplyModifiedProperties();
    }
}