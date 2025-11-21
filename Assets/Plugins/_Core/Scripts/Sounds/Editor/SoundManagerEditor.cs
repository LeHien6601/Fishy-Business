using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SoundManagerEditor : EditorWindow
{
    private Vector2 scrollPos;
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Sound Groups", "All Sounds" };

    // Tab 1: Group & Type Management
    

    private List<SoundGroupData> soundGroups = new();

    // Cache all SoundData assets
    private SoundData[] allSoundDatas;
    private Editor[] soundDataPreviewEditors;

    private const string DATA_PATH = "Assets/Resources/SoundGroups.json";

    [MenuItem("MyGame/Sound Manager Editor")]
    public static void OpenWindow()
    {
        GetWindow<SoundManagerEditor>("Sound Manager");
    }

    private void OnEnable()
    {
        LoadGroupData();
        RefreshSoundDataList();
    }

    private void OnGUI()
    {
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (selectedTab)
        {
            case 0:
                DrawSoundGroupsTab();
                break;
            case 1:
                DrawAllSoundsTab();
                break;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Save All Changes", GUILayout.Height(30)))
        {
            SaveGroupData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Sound Manager: All changes saved!");
        }
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

    // private void DrawAllSoundsTab()
    // {
    //     if (allSoundDatas == null || allSoundDatas.Length == 0)
    //     {
    //         EditorGUILayout.HelpBox("No SoundData assets found!", MessageType.Info);
    //         if (GUILayout.Button("Create New SoundData"))
    //             CreateNewSoundData();
    //         return;
    //     }

    //     EditorGUILayout.LabelField($"Found {allSoundDatas.Length} SoundData(s)", EditorStyles.boldLabel);

    //     if (GUILayout.Button("Refresh List", GUILayout.Height(25)))
    //         RefreshSoundDataList();

    //     EditorGUILayout.Space(8);

    //     for (int i = 0; i < allSoundDatas.Length; i++)
    //     {
    //         var soundData = allSoundDatas[i];
    //         if (soundData == null) continue;

    //         EditorGUILayout.BeginVertical("box");
    //         EditorGUILayout.BeginHorizontal();
    //         EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(soundData)), EditorStyles.boldLabel);
    //         if (GUILayout.Button("Ping", GUILayout.Width(40))) EditorGUIUtility.PingObject(soundData);
    //         EditorGUILayout.EndHorizontal();

    //         // === Preview Row ===
    //         if (soundDataPreviewEditors[i] == null)
    //             soundDataPreviewEditors[i] = Editor.CreateEditor(soundData, typeof(SoundDataEditor));

    //         var editor = soundDataPreviewEditors[i] as SoundDataEditor;
    //         if (editor != null)
    //         {
    //             AudioClip previewClip = soundData.clips[soundData.sequenceIndex % soundData.clips.Count];
    //             EditorGUILayout.BeginHorizontal();
    //             editor.DrawPreviewControls();

    //             if (previewClip != null)
    //                 editor.DrawClipPreview(previewClip);
    //             else
    //                 EditorGUILayout.HelpBox("No clip assigned", MessageType.Warning);
    //             EditorGUILayout.EndHorizontal();
    //         }

    //         // === Group & Type Row (Inline Popups) ===
    //         EditorGUILayout.BeginHorizontal();
            
    //         EditorGUI.BeginChangeCheck();

    //         // Group Popup
    //         int selectedGroupIndex = soundGroups.FindIndex(g => g.groupName == soundData.soundGroup);
    //         if (selectedGroupIndex == -1) selectedGroupIndex = 0;
    //         string[] groupNames = soundGroups.Select(g => g.groupName).Prepend("Uncategorized").ToArray();
    //         int newGroupIndex = EditorGUILayout.Popup(
    //             selectedGroupIndex < 0 ? 0 : selectedGroupIndex + 1,
    //             groupNames,
    //             GUILayout.Width(140)
    //         );
    //         string newGroup = newGroupIndex == 0 ? "Uncategorized" : groupNames[newGroupIndex];

    //         GUILayout.Space(8);

    //         // Type Popup (based on selected group)
    //         List<string> typeOptions = newGroup == "Uncategorized" 
    //             ? new List<string> { "Default" } 
    //             : soundGroups.Find(g => g.groupName == newGroup)?.soundTypes ?? new List<string> { "Default" };

    //         int selectedTypeIndex = typeOptions.IndexOf(soundData.soundType);
    //         if (selectedTypeIndex == -1) selectedTypeIndex = 0;

    //         int newTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeOptions.ToArray());
    //         string newType = typeOptions[Mathf.Max(0, newTypeIndex)];

    //         if (EditorGUI.EndChangeCheck())
    //         {
    //             soundData.soundGroup = newGroup;
    //             soundData.soundType = newType;
    //             EditorUtility.SetDirty(soundData);
    //         }

    //         EditorGUILayout.EndHorizontal();

    //         EditorGUILayout.EndVertical();
    //         EditorGUILayout.Space(6);
    //     }
    // }

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
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

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
                        EditorGUILayout.EndScrollView();
                        return; // Early exit to avoid invalid index access
                    }
                }

                // === Editable Name Field ===
                EditorGUI.BeginChangeCheck();
                string newName = EditorGUILayout.TextField(soundData.name, EditorStyles.boldLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    string oldPath = AssetDatabase.GetAssetPath(soundData);
                    string folder = Path.GetDirectoryName(oldPath);
                    string extension = Path.GetExtension(oldPath);
                    string newPath = $"{folder}/{newName}{extension}";

                    // Validate filename
                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        EditorUtility.DisplayDialog("Invalid Name", "SoundData name cannot be empty.", "OK");
                    }
                    else if (AssetDatabase.LoadAssetAtPath<SoundData>(newPath) != null && newPath != oldPath)
                    {
                        EditorUtility.DisplayDialog("Name Conflict", "A SoundData with this name already exists in the folder.", "OK");
                    }
                    else
                    {
                        soundData.name = newName; // Update object name
                        AssetDatabase.RenameAsset(oldPath, newName);
                        EditorUtility.SetDirty(soundData);
                    }
                }

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
                    Undo.RecordObject(soundData, "Change Sound Group/Type");
                    soundData.soundGroup = newGroup;
                    soundData.soundType = newType;
                    EditorUtility.SetDirty(soundData);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
            }
        }

        EditorGUILayout.EndScrollView();

        // === Bottom Button: Always Visible ===
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Create New SoundData", GUILayout.Height(40)))
        {
            CreateNewSoundData();
        }
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