using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundData))]
public class SoundDataEditor : Editor
{
    private AudioSource previewSource;
    private SoundData soundData;
    private bool isPlaying = false;
    private float realTimePlaying = 0f;        
    private float lastRealTime = 0f;        
    private bool wasPlayingLastFrame = false;
    private const string DATA_PATH = "Assets/Resources/SoundGroups.json";
    private List<SoundGroupData> soundGroups = new();

    private void OnEnable()
    {
        soundData = (SoundData)target;
        LoadGroupData();
        CreatePreviewAudioSource();
        EditorApplication.update += UpdatePreview;
    }

    private void OnDisable()
    {
        StopPreview();
        DestroyPreviewAudioSource();
        EditorApplication.update -= UpdatePreview;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // === Standard fields ===
        EditorGUILayout.PropertyField(serializedObject.FindProperty("clips"), true);

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        float minDB = serializedObject.FindProperty("minVolumeDB").floatValue;
        float maxDB = serializedObject.FindProperty("maxVolumeDB").floatValue;
        EditorGUILayout.MinMaxSlider(
            "Volume Range",
            ref minDB, ref maxDB,
            -40f, 40f);

        // Draw two handles with labels
        Rect r = GUILayoutUtility.GetLastRect();
        float indent = EditorGUI.indentLevel * 15f;
        GUI.Label(new Rect(r.x + indent + 5, r.y + 15, 80, 20), $"{minDB:F2} dB");
        GUI.Label(new Rect(r.xMax - 85, r.y + 15, 80, 20), $"{maxDB:F2} dB");
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.FindProperty("minVolumeDB").floatValue = minDB;
            serializedObject.FindProperty("maxVolumeDB").floatValue = Mathf.Max(minDB, maxDB);
        }

        EditorGUILayout.Space(10);
        EditorGUI.BeginChangeCheck();
        float minPitch = serializedObject.FindProperty("minPitch").floatValue;
        float maxPitch = serializedObject.FindProperty("maxPitch").floatValue;
        EditorGUILayout.MinMaxSlider(
            "Pitch Range",
            ref minPitch, ref maxPitch,
            0f, 2f);
        // Draw two handles with labels
        Rect r1 = GUILayoutUtility.GetLastRect();
        float indent1 = EditorGUI.indentLevel * 15f;
        GUI.Label(new Rect(r1.x + indent1 + 5, r1.y + 15, 80, 20), $"{minPitch:F3}");
        GUI.Label(new Rect(r1.xMax - 85, r1.y + 15, 80, 20), $"{maxPitch:F3}");
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.FindProperty("minPitch").floatValue = minPitch;
            serializedObject.FindProperty("maxPitch").floatValue = Mathf.Max(minPitch, maxPitch);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playbackMode"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        if (soundData.clips == null || soundData.clips.Count == 0 || soundData.clips[0] == null)
        {
            EditorGUILayout.HelpBox("Add at least one AudioClip to preview.", MessageType.Info);
            return;
        }

        // === Draw the currently selected clip's seek bar ===
        AudioClip currentClip = soundData.clips[soundData.sequenceIndex % soundData.clips.Count]; 
        if (currentClip != null)
        {
            EditorGUILayout.BeginHorizontal();
            DrawPreviewControls();
            DrawClipPreview(currentClip);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
            
        EditorGUI.BeginChangeCheck();

        // Group Popup
        int selectedGroupIndex = soundGroups.FindIndex(g => g.groupName == soundData.soundGroup);
        if (selectedGroupIndex == -1) selectedGroupIndex = 0;
        string[] groupNames = soundGroups.Select(g => g.groupName).Prepend("Uncategorized").ToArray();
        int newGroupIndex = EditorGUILayout.Popup(
            selectedGroupIndex < 0 ? 0 : selectedGroupIndex + 1,
            groupNames,
            GUILayout.Width(140)
        );
        string newGroup = newGroupIndex == 0 ? "Uncategorized" : groupNames[newGroupIndex];

        GUILayout.Space(8);

        // Type Popup (based on selected group)
        List<string> typeOptions = newGroup == "Uncategorized" 
            ? new List<string> { "Default" } 
            : soundGroups.Find(g => g.groupName == newGroup)?.soundTypes ?? new List<string> { "Default" };

        int selectedTypeIndex = typeOptions.IndexOf(soundData.soundType);
        if (selectedTypeIndex == -1) selectedTypeIndex = 0;

        int newTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeOptions.ToArray());
        string newType = typeOptions[Mathf.Max(0, newTypeIndex)];

        if (EditorGUI.EndChangeCheck())
        {
            ApplyGroupAndType(soundData, newGroup, newType);
        }

        EditorGUILayout.EndHorizontal();
    }

    public void DrawPreviewControls()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(26));
        
        // Play/Pause Icon Button
        Texture playIcon = EditorGUIUtility.IconContent(isPlaying ? "PauseButton On" : "PlayButton").image;
        Texture pauseIcon = EditorGUIUtility.IconContent("PauseButton").image;
        if (GUILayout.Button(isPlaying ? pauseIcon : playIcon, GUIStyle.none, GUILayout.Width(24), GUILayout.Height(24)))
        {
            if (isPlaying) PausePreview();
            else PlayPreview();
        }

        // Stop Button
        if (GUILayout.Button(EditorGUIUtility.IconContent("StopButton").image, GUIStyle.none, GUILayout.Width(24), GUILayout.Height(24)))
        {
            StopPreview();
        }

        GUILayout.Space(8);
        EditorGUILayout.EndHorizontal();
    }

    public void DrawClipPreview(AudioClip clip)
    {
        if (clip == null || previewSource == null) return;

        float baseDuration = clip.length;
        float currentPitch = previewSource.pitch;
        float perceivedDuration = baseDuration / Mathf.Max(currentPitch, 0.001f); // Avoid divide by zero
        float currentRealTime = realTimePlaying;
        // Clamp real time to perceived duration
        currentRealTime = Mathf.Clamp(currentRealTime, 0f, perceivedDuration);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        float newTime = EditorGUILayout.Slider(currentRealTime / Mathf.Max(currentPitch, 0.001f), 0f, perceivedDuration, GUILayout.Height(20)) * Mathf.Max(currentPitch, 0.001f);
        if (EditorGUI.EndChangeCheck())
        {
            realTimePlaying = newTime;

            // Convert real time back to sample time for AudioSource
            float sampleTime = newTime * currentPitch;
            previewSource.time = Mathf.Clamp(sampleTime, 0f, baseDuration);

            if (!isPlaying)
            {
                previewSource.UnPause();
                previewSource.Pause();
            }
        }

        EditorGUILayout.EndHorizontal();

        // Show real-world time!
        EditorGUILayout.LabelField($"{FormatTime(currentRealTime / Mathf.Max(currentPitch, 0.001f))} / {FormatTime(perceivedDuration)}", 
            EditorStyles.miniLabel);
    }

    private string FormatTime(float seconds)
    {
        int mins = (int)seconds / 60;
        int secs = (int)seconds % 60;
        int ms = (int)(seconds * 1000) % 1000;
        return $"{mins:00}:{secs:00}.{ms:000}";
    }

    // === Preview AudioSource Management ===
    private void CreatePreviewAudioSource()
    {
        if (previewSource == null)
        {
            GameObject go = EditorUtility.CreateGameObjectWithHideFlags(
                "SoundData_Preview", HideFlags.HideAndDontSave);
            previewSource = go.AddComponent<AudioSource>();
            previewSource.playOnAwake = false;
            previewSource.loop = false;
        }
    }

    private void DestroyPreviewAudioSource()
    {
        if (previewSource != null)
        {
            DestroyImmediate(previewSource.gameObject);
            previewSource = null;
        }
    }

    private void PlayPreview()
    {
        if (soundData.clips.Count == 0 || soundData.clips[0] == null) return;

        AudioClip clip = soundData.GetNextClip(); 
        previewSource.clip = clip;
        previewSource.volume = soundData.GetRandomVolume();
        previewSource.pitch = soundData.GetRandomPitch();
        previewSource.time = 0f;
        lastRealTime = (float)EditorApplication.timeSinceStartup;
        previewSource.Play();
        wasPlayingLastFrame = true;
        isPlaying = true;
    }

    private void PausePreview()
    {
        if (previewSource != null) previewSource.Pause();
        isPlaying = false;
        wasPlayingLastFrame = false;
    }

    private void StopPreview()
    {
        if (previewSource != null) previewSource.Stop();
        isPlaying = false;
        wasPlayingLastFrame = false;
        realTimePlaying = 0f;
    }

    private void UpdatePreview()
    {
        if (previewSource == null || previewSource.clip == null) return;

        bool currentlyPlaying = previewSource.isPlaying;
        float baseDuration = previewSource.clip.length;

        if (isPlaying && currentlyPlaying)
        {
            float now = (float)EditorApplication.timeSinceStartup;
            float delta = now - lastRealTime;
            realTimePlaying += delta * previewSource.pitch;
            lastRealTime = now;

            // Auto-stop when perceived duration is reached
            if (realTimePlaying >= baseDuration)
            {
                realTimePlaying = 0f;
                StopPreview();
                Repaint();
            }

            wasPlayingLastFrame = true;
        }
        else if (wasPlayingLastFrame && !currentlyPlaying)
        {
            isPlaying = false;
            wasPlayingLastFrame = false;
            realTimePlaying = 0f;
            Repaint();
        }
    }

    private void LoadGroupData()
    {
        if (File.Exists(DATA_PATH))
        {
            string json = File.ReadAllText(DATA_PATH);
            var save = JsonUtility.FromJson<SoundDatabaseEditor.SaveData>(json);
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
    static public void ApplyGroupAndType(SoundData soundData, string newGroup, string newType)
    {
        if (soundData.soundGroup == newGroup && soundData.soundType == newType)
            return;
        Undo.RecordObject(soundData, "Change Sound Group/Type");
        soundData.soundGroup = newGroup;
        soundData.soundType = newType;
        string newAssetName = $"{newGroup}_{newType}";
        string oldPath = AssetDatabase.GetAssetPath(soundData);
        string folder = Path.GetDirectoryName(oldPath);
        string newPath = Path.Combine(folder, newAssetName + ".asset");
        if (AssetDatabase.LoadAssetAtPath<SoundData>(newPath) != null && newPath != oldPath)
        {
            Debug.LogWarning($"Cannot rename: '{newAssetName}' already exists.");
        }
        else
        {
            AssetDatabase.RenameAsset(oldPath, newAssetName);
        }
        EditorUtility.SetDirty(soundData);
    }
    public override bool RequiresConstantRepaint() => isPlaying;
}
