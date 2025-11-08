using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public class GameConfigEditor : EditorWindow
{
    private GameConfig config;
    private SerializedObject serializedConfig;
    private int currentTab = 0;
    private string[] tabNames = { "UIView", "PlayerIcons" }; // Customize tabs here

    [MenuItem("MyGame/Config Editor")] // This adds the menu item under a new "MyGame" tab in the menu bar
    public static void OpenWindow()
    {
        GetWindow<GameConfigEditor>("Game Config");
    }
    private Vector2 scrollPosition; // For scrollable table
    private void OnEnable()
    {
        // Load the config when the window opens
        config = GameConfig.Instance;
        if (config != null)
        {
            serializedConfig = new SerializedObject(config);
        }
        else
        {
            Debug.LogError("Failed to load GameConfig!");
        }
    }

    private void OnGUI()
    {
        if (config == null) 
        {
            EditorGUILayout.LabelField("No GameConfig loaded!", EditorStyles.boldLabel);
            return;
        }

        // Draw the tab bar
        currentTab = GUILayout.Toolbar(currentTab, tabNames);

        // Begin editing the serialized object for undo/redo support
        serializedConfig.Update();

        // Draw fields based on the current tab
        EditorGUILayout.BeginVertical("box");
        switch (currentTab)
        {
            case 0: // UIView tab
                DrawUIViewsTable();
                break;
            case 1: // Player Icons tab
                DrawProperty("playerIcons");
                break;
        }
        EditorGUILayout.EndVertical();

        // Apply changes and mark as dirty for saving
        if (serializedConfig.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(config);
        }

        // Optional: Save button
        if (GUILayout.Button("Save Config"))
        {
            AssetDatabase.SaveAssets();
            Debug.Log("GameConfig saved!");
        }
    }

    private void DrawProperty(string propertyName)
    {
        SerializedProperty prop = serializedConfig.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.PropertyField(prop, true);
        }
    }

    private void DrawUIViewsTable()
    {
        SerializedProperty uiViewProp = serializedConfig.FindProperty("uiViewPrefabs");
        if (uiViewProp == null) return;

        // Header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("State", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("View", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("Order", EditorStyles.boldLabel, GUILayout.Width(50));
        GUILayout.Label("Actions", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        // Scroll view for table content
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < uiViewProp.arraySize; i++)
        {
            SerializedProperty itemProp = uiViewProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginHorizontal();

            // Draw fields for each item
            SerializedProperty stateProp = itemProp.FindPropertyRelative("State");
            SerializedProperty viewProp = itemProp.FindPropertyRelative("View");
            SerializedProperty orderProp = itemProp.FindPropertyRelative("SortingOrder");

            EditorGUILayout.PropertyField(stateProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.PropertyField(viewProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.PropertyField(orderProp, GUIContent.none, GUILayout.Width(50));

            // Delete button
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                uiViewProp.DeleteArrayElementAtIndex(i);
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // Add new item button
        if (GUILayout.Button("Add Item"))
        {
            uiViewProp.arraySize++;
            // Initialize new item
            SerializedProperty newItem = uiViewProp.GetArrayElementAtIndex(uiViewProp.arraySize - 1);
            newItem.FindPropertyRelative("State").enumValueIndex = 0;
            newItem.FindPropertyRelative("View").objectReferenceValue = null;
            newItem.FindPropertyRelative("SortingOrder").intValue = 0;
        }
    }
}