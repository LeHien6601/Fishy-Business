using UnityEngine;
using System.Collections.Generic;
using HHDCore;

public class SoundManager : SingletonMono<SoundManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfx2DSource;        // For UI / non-spatial
    [SerializeField] private AudioSource musicSource;

    [Header("3D Sound Settings")]
    [SerializeField] private int initialPoolSize = 15;
    [SerializeField] private int maxPoolSize = 50;

    private Dictionary<SoundType, SoundData> soundMap;
    private readonly Queue<AudioSource> sourcePool = new();
    private readonly List<AudioSource> activeSources = new();

    public override void Awake()
    {
        base.Awake();
        BuildSoundMap();
        InitializePool();
    }

    private void InitializePool()
    {
        var poolParent = new GameObject("[3D Sound Pool]").transform;
        poolParent.parent = transform;
        DontDestroyOnLoad(poolParent.gameObject);

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledSource(poolParent);
        }
    }

    private AudioSource CreatePooledSource(Transform parent)
    {
        var go = new GameObject("Pooled3DAudioSource");
        go.transform.parent = parent;
        go.SetActive(false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;           // Fully 3D
        source.dopplerLevel = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 1f;
        source.maxDistance = 25f;

        sourcePool.Enqueue(source);
        return source;
    }

    private AudioSource GetAvailableSource()
    {
        // Reuse from pool
        if (sourcePool.Count > 0)
        {
            var source = sourcePool.Dequeue();
            source.gameObject.SetActive(true);
            activeSources.Add(source);
            return source;
        }

        // Create new if under max limit
        if (activeSources.Count + sourcePool.Count < maxPoolSize)
        {
            var source = CreatePooledSource(transform);
            source.gameObject.SetActive(true);
            activeSources.Add(source);
            return source;
        }

        // Fallback: reuse oldest active source (least recently used)
        var oldest = activeSources[0];
        oldest.Stop();
        return oldest;
    }

    private void ReturnSourceToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        activeSources.Remove(source);
        sourcePool.Enqueue(source);
        source.transform.position = Vector3.zero;
    }

    private void Update()
    {
        // Return finished sources to pool
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            var source = activeSources[i];
            if (!source.isPlaying)
            {
                ReturnSourceToPool(source);
            }
        }
    }

    private void BuildSoundMap()
    {
        soundMap = new Dictionary<SoundType, SoundData>();

        var config = GameConfig.Instance;
        if (config == null)
        {
            Debug.LogError("GameConfig not found!");
            return;
        }

        foreach (var mapping in config.soundMappings)
        {
            if (mapping.soundData != null && !soundMap.ContainsKey(mapping.soundType))
            {
                soundMap[mapping.soundType] = mapping.soundData;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // MAIN PLAY METHODS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Play a sound at a world position (3D spatialized)
    /// </summary>
    public static void Play(SoundType type, Vector3 position)
    {
        if (Instance == null) return;
        Instance.InternalPlay(type, position, true);
    }

    /// <summary>
    /// Play a 2D sound (UI, music, etc.)
    /// </summary>
    public static void Play2D(SoundType type)
    {
        if (Instance == null || Instance.sfx2DSource == null) return;
        Instance.InternalPlay(type, Vector3.zero, false);
    }

    /// <summary>
    /// Play music (looping, 2D)
    /// </summary>
    public static void PlayMusic(SoundType type)
    {
        if (Instance == null || Instance.musicSource == null) return;

        if (!Instance.soundMap.TryGetValue(type, out SoundData data) || data == null)
        {
            Debug.LogWarning($"No SoundData for music: {type}");
            return;
        }

        var clip = data.GetNextClip();
        if (clip == null) return;

        Instance.musicSource.Stop();
        Instance.musicSource.clip = clip;
        Instance.musicSource.volume = data.GetRandomVolume();
        Instance.musicSource.pitch = data.GetRandomPitch();
        Instance.musicSource.loop = true;
        Instance.musicSource.Play();
    }

    // ─────────────────────────────────────────────────────────────────────

    private void InternalPlay(SoundType type, Vector3 worldPos, bool is3D)
    {
        if (!soundMap.TryGetValue(type, out SoundData soundData) || soundData == null)
        {
            Debug.LogWarning($"No SoundData mapped for SoundType: {type}");
            return;
        }

        var clip = soundData.GetNextClip();
        if (clip == null)
        {
            Debug.LogWarning($"No clip in SoundData: {soundData.name}");
            return;
        }

        AudioSource source;

        if (is3D)
        {
            source = GetAvailableSource();
            source.transform.position = worldPos;
            source.spatialBlend = 1f;
        }
        else
        {
            source = sfx2DSource;
            source.spatialBlend = 0f; // Force 2D
        }

        source.clip = clip;
        source.volume = soundData.GetRandomVolume();
        source.pitch = soundData.GetRandomPitch();
        source.loop = false;

        source.Play();

        // For 2D source, no need to track — it's always active
        if (!is3D)
            return;

        // Auto-return when done (handled in Update)
    }

    // Optional: Rebuild mapping at runtime
    public void RebuildMap() => BuildSoundMap();
}