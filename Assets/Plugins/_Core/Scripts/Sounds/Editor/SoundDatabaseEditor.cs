using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SoundDatabaseEditor : EditorWindow
{
    private Vector2 scrollPos;
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Sound Groups", "All Sounds", "Sound Types (Enum)" };
    private List<SoundGroupData> soundGroups = new();
    private SoundData[] allSoundDatas;
    private Editor[] soundDataPreviewEditors;

    private const string DATA_PATH = "Assets/Resources/SoundGroups.json";
    private const string ENUM_FILE_PATH = "Assets/Plugins/_Core/Scripts/Sounds/SoundType.cs";

    // === ENUM BUFFER SYSTEM ===
    private List<string> originalEnumValues;
    private List<string> bufferedEnumValues;  // Working copy
    private bool hasPendingEnumChanges = false;

    [MenuItem("MyGame/Sound Manager Editor")]
    public static void OpenWindow()
    {
        GetWindow<SoundDatabaseEditor>("Sound Manager");
    }

    private void OnEnable()
    {
        LoadGroupData();
        RefreshSoundDataList();
        LoadCurrentEnumValues();
    }

    private void LoadCurrentEnumValues()
    {
        originalEnumValues = Enum.GetNames(typeof(SoundType)).ToList();
        bufferedEnumValues = new List<string>(originalEnumValues);
        hasPendingEnumChanges = false;
    }

    private void OnGUI()
    {
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        switch (selectedTab)
        {
            case 0: DrawSoundGroupsTab(); break;
            case 1: DrawAllSoundsTab(); break;
            case 2: DrawSoundTypesTab(); break;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // === Global Save Button + Enum Apply/Discard ===
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Groups & Assets", GUILayout.Height(30)))
        {
            SaveGroupData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Sound Groups & Assets saved!");
        }

        EditorGUI.BeginDisabledGroup(!hasPendingEnumChanges);
        if (GUILayout.Button("Apply Enum Changes", GUILayout.Height(30)))
        {
            ApplyEnumChanges();
        }
        if (GUILayout.Button("Discard Enum Changes", GUILayout.Height(30)))
        {
            LoadCurrentEnumValues();
            Repaint();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        if (hasPendingEnumChanges)
        {
            EditorGUILayout.HelpBox($"Warning: {CountPendingChanges()} pending changes to SoundType enum. Click 'Apply' to save.", MessageType.Warning);
        }
    }
    private int CountPendingChanges()
    {
        int added = bufferedEnumValues.Count(v => v != "None" && !originalEnumValues.Contains(v));
        int removed = originalEnumValues.Count(v => v != "None" && !bufferedEnumValues.Contains(v));
        int renamed = 0;

        foreach (var oldName in originalEnumValues)
        {
            if (oldName == "None") continue;
            if (!bufferedEnumValues.Contains(oldName)) renamed++;
        }

        return added + removed + renamed;
    }

    #region Tab 1: Sound Groups

    private void DrawSoundGroupsTab()
    {
        EditorGUILayout.LabelField("Manage Sound Groups & Types", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        for (int g = 0; g < soundGroups.Count; g++)
        {
            var group = soundGroups[g];
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            group.groupName = EditorGUILayout.TextField(group.groupName, GUILayout.Width(200));
            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, "", true, EditorStyles.foldoutHeader);

            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
                if (EditorUtility.DisplayDialog("Delete Group", $"Delete group '{group.groupName}'?", "Yes", "No"))
                {
                    soundGroups.RemoveAt(g);
                    g--;
                    SaveGroupData();
                    continue;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (group.isExpanded)
            {
                EditorGUI.indentLevel++;
                for (int t = 0; t < group.soundTypes.Count; t++)
                {
                    EditorGUILayout.BeginHorizontal();
                    group.soundTypes[t] = EditorGUILayout.TextField(group.soundTypes[t]);
                    if (GUILayout.Button("-", GUILayout.Width(30)))
                    {
                        group.soundTypes.RemoveAt(t);
                        t--;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                if (GUILayout.Button("+ Add Sound Type", GUILayout.Width(150)))
                {
                    group.soundTypes.Add("NewType");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Create New Group", GUILayout.Height(35)))
        {
            soundGroups.Add(new SoundGroupData { groupName = "New Group" });
        }
    }

    #endregion

    #region Tab 2: All SoundData Assets
    private void DrawAllSoundsTab()
    {
        if (allSoundDatas == null || allSoundDatas.Length == 0)
        {
            EditorGUILayout.HelpBox("No SoundData assets found!", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Found {allSoundDatas.Length} SoundData(s)", EditorStyles.boldLabel);

            if (GUILayout.Button("Refresh List", GUILayout.Height(25)))
                RefreshSoundDataList();

            EditorGUILayout.Space(8);
        }

        // Start scrollable list
        // scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (allSoundDatas != null)
        {
            for (int i = 0; i < allSoundDatas.Length; i++)
            {
                var soundData = allSoundDatas[i];
                if (soundData == null) continue;

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                // === Delete Button (Top-Left) ===
                if (GUILayout.Button("–", GUILayout.Width(24), GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("Delete SoundData",
                        $"Are you sure you want to delete '{soundData.name}'?\nThis cannot be undone.",
                        "Delete", "Cancel"))
                    {
                        string path = AssetDatabase.GetAssetPath(soundData);
                        AssetDatabase.DeleteAsset(path);
                        RefreshSoundDataList();
                        EditorGUILayout.EndVertical();
                        // EditorGUILayout.EndScrollView();
                        return; // Early exit to avoid invalid index access
                    }
                }

                // === Editable Name Field ===
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField(soundData.name, EditorStyles.boldLabel);

                // === Ping Button ===
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    EditorGUIUtility.PingObject(soundData);

                EditorGUILayout.EndHorizontal();

                // === Preview Row ===
                if (soundDataPreviewEditors[i] == null)
                    soundDataPreviewEditors[i] = Editor.CreateEditor(soundData, typeof(SoundDataEditor));

                var editor = soundDataPreviewEditors[i] as SoundDataEditor;
                if (editor != null)
                {
                    AudioClip previewClip = soundData.clips.Count > 0 ? soundData.clips[soundData.sequenceIndex % soundData.clips.Count] : null;
                    EditorGUILayout.BeginHorizontal();
                    editor.DrawPreviewControls();

                    if (previewClip != null)
                        editor.DrawClipPreview(previewClip);
                    else
                        EditorGUILayout.HelpBox("No clip assigned", MessageType.Warning);
                    EditorGUILayout.EndHorizontal();
                }

                // === Group & Type Row ===
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();

                // Group Popup
                int selectedGroupIndex = soundGroups.FindIndex(g => g.groupName == soundData.soundGroup);
                string[] groupNames = soundGroups.Select(g => g.groupName).Prepend("Uncategorized").ToArray();
                int newGroupIndex = EditorGUILayout.Popup(
                    selectedGroupIndex < 0 ? 0 : selectedGroupIndex + 1,
                    groupNames,
                    GUILayout.Width(140)
                );
                string newGroup = newGroupIndex == 0 ? "Uncategorized" : groupNames[newGroupIndex];

                GUILayout.Space(8);

                // Type Popup
                List<string> typeOptions = newGroup == "Uncategorized"
                    ? new List<string> { "Default" }
                    : soundGroups.Find(g => g.groupName == newGroup)?.soundTypes ?? new List<string> { "Default" };

                int selectedTypeIndex = typeOptions.IndexOf(soundData.soundType);
                if (selectedTypeIndex == -1) selectedTypeIndex = 0;

                int newTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeOptions.ToArray());
                string newType = typeOptions[Mathf.Max(0, newTypeIndex)];

                if (EditorGUI.EndChangeCheck())
                {
                    SoundDataEditor.ApplyGroupAndType(soundData, newGroup, newType);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
            }
        }

        // EditorGUILayout.EndScrollView();

        // === Bottom Button: Always Visible ===
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Create New SoundData", GUILayout.Height(40)))
        {
            CreateNewSoundData();
        }
    }

    #endregion

    #region Tab 3: SoundType Enum (Buffered)
    private void DrawSoundTypesTab()
    {
        EditorGUILayout.LabelField("Manage SoundType Enum Values", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Changes are buffered. Click 'Apply Enum Changes' to write to file.", MessageType.Info);

        EditorGUILayout.Space();

        // Show list from buffered values
        for (int i = 0; i < bufferedEnumValues.Count; i++)
        {
            string value = bufferedEnumValues[i];
            if (value == "None") 
            {
                EditorGUILayout.LabelField("    None,", EditorStyles.miniLabel);
                continue;
            }

            EditorGUILayout.BeginHorizontal();

            string newName = EditorGUILayout.TextField(value, GUILayout.Width(220));

            if (GUILayout.Button("−", GUILayout.Width(30)))
            {
                if (EditorUtility.DisplayDialog("Remove SoundType", 
                    $"Remove '{value}' from SoundType enum?", "Remove", "Cancel"))
                {
                    bufferedEnumValues.RemoveAt(i);
                    hasPendingEnumChanges = true;
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();

            // Rename handling
            if (newName != value && !string.IsNullOrWhiteSpace(newName))
            {
                if (bufferedEnumValues.Contains(newName))
                {
                    EditorUtility.DisplayDialog("Duplicate", $"SoundType '{newName}' already exists!", "OK");
                }
                else if (!IsValidIdentifier(newName))
                {
                    EditorUtility.DisplayDialog("Invalid", "Must be valid C# identifier (letters, digits, _)", "OK");
                }
                else
                {
                    bufferedEnumValues[i] = newName;
                    hasPendingEnumChanges = true;
                    Repaint();
                }
            }
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("+ Add New SoundType", GUILayout.Height(35)))
        {
            string newName = "NewSound";
            int counter = 1;
            while (bufferedEnumValues.Contains(newName + (counter > 1 ? counter.ToString() : "")))
                counter++;
            if (counter > 1) newName += counter;

            bufferedEnumValues.Add(newName);
            hasPendingEnumChanges = true;
            Repaint();
        }
    }

    private void ApplyEnumChanges()
    {
        if (!EditorUtility.DisplayDialog("Apply SoundType Changes",
            $"This will rewrite SoundType.cs with {bufferedEnumValues.Count(v => v != "None")} entries.\n\nContinue?", 
            "Apply", "Cancel"))
            return;

        RewriteEnumFile(bufferedEnumValues);
        originalEnumValues = new List<string>(bufferedEnumValues);
        hasPendingEnumChanges = false;
        Debug.Log("SoundType.cs successfully updated!");
    }

    private void RewriteEnumFile(List<string> values)
    {
        var lines = new List<string>
        {
            "public enum SoundType",
            "{",
            "    None,"
        };

        foreach (var val in values)
        {
            if (val != "None")
                lines.Add($"    {val},");
        }

        lines.Add("}");

        File.WriteAllLines(ENUM_FILE_PATH, lines);
        AssetDatabase.Refresh();
    }

    private bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    #endregion

    #region Data Persistence

    [Serializable]
    public class SaveData
    {
        public List<SoundGroupData> groups = new List<SoundGroupData>();
    }

    private void LoadGroupData()
    {
        if (File.Exists(DATA_PATH))
        {
            string json = File.ReadAllText(DATA_PATH);
            var save = JsonUtility.FromJson<SaveData>(json);
            soundGroups = save.groups ?? new List<SoundGroupData>();
        }

        if (soundGroups.Count == 0)
        {
            soundGroups.Add(new SoundGroupData
            {
                groupName = "SFX",
                soundTypes = new List<string> { "UI", "Footstep", "Weapon", "Explosion" }
            });
            soundGroups.Add(new SoundGroupData
            {
                groupName = "Music",
                soundTypes = new List<string> { "Menu", "Gameplay", "Boss" }
            });
        }
    }

    private void SaveGroupData()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DATA_PATH));
        var save = new SaveData { groups = soundGroups };
        string json = JsonUtility.ToJson(save, true);
        File.WriteAllText(DATA_PATH, json);
    }

    #endregion

    #region Utility

    private void RefreshSoundDataList()
    {
        string[] guids = AssetDatabase.FindAssets("t:SoundData");
        allSoundDatas = new SoundData[guids.Length];
        soundDataPreviewEditors = new Editor[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            allSoundDatas[i] = AssetDatabase.LoadAssetAtPath<SoundData>(path);
        }
    }

    private void CreateNewSoundData()
    {
        var newAsset = ScriptableObject.CreateInstance<SoundData>();
        string folder = "Assets/Resources/Sounds";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Sounds");

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/New_SoundData.asset");
        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(newAsset);
        RefreshSoundDataList();
    }
    #endregion
}
[Serializable]
public class SoundGroupData
{
    public string groupName = "New Group";
    public bool isExpanded = true;
    public List<string> soundTypes = new(){ "Default" };
}