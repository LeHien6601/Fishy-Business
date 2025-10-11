using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIAnimation))]
public class UIAnimationEditor : Editor
{
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Rotate", "Move", "Scale", "Fade" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

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
}