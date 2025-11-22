// SoundManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using HHDCore;

public class SoundManager : SingletonMono<SoundManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;

    [Header("Pooling")]
    [SerializeField] private int initial2DPoolSize = 10;
    [SerializeField] private int initial3DPoolSize = 15;
    [SerializeField] private int maxPoolSize = 60;

    private Dictionary<SoundType, SoundData> soundMap;

    // Separate pools
    private readonly Queue<AudioSource> pool2D = new();
    private readonly Queue<AudioSource> pool3D = new();
    private readonly List<AudioSource> active2DSources = new();
    private readonly List<AudioSource> active3DSources = new();

    private Transform poolParent;

    public override void Awake()
    {
        base.Awake();
        BuildSoundMap();
        InitializePools();
    }

    private void InitializePools()
    {
        poolParent = new GameObject("[Sound Pool]").transform;
        poolParent.parent = transform;
        DontDestroyOnLoad(poolParent.gameObject);

        for (int i = 0; i < initial2DPoolSize; i++) CreatePooledSource(false);
        for (int i = 0; i < initial3DPoolSize; i++) CreatePooledSource(true);
    }

    private AudioSource CreatePooledSource(bool is3D)
    {
        var go = new GameObject(is3D ? "3D_SFX" : "2D_SFX");
        go.transform.parent = poolParent;
        go.SetActive(false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = is3D ? 1f : 0f;
        source.dopplerLevel = is3D ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 1f;
        source.maxDistance = 25f;

        if (is3D)
            pool3D.Enqueue(source);
        else
            pool2D.Enqueue(source);

        return source;
    }

    private AudioSource GetSource(bool is3D)
    {
        var pool = is3D ? pool3D : pool2D;
        var activeList = is3D ? active3DSources : active2DSources;

        if (pool.Count > 0)
        {
            var source = pool.Dequeue();
            source.gameObject.SetActive(true);
            activeList.Add(source);
            return source;
        }

        if (active2DSources.Count + active3DSources.Count + pool2D.Count + pool3D.Count < maxPoolSize)
        {
            var source = CreatePooledSource(is3D);
            source.gameObject.SetActive(true);
            activeList.Add(source);
            return source;
        }

        // Fallback: steal oldest from same category
        var oldest = activeList[0];
        oldest.Stop();
        return oldest;
    }

    private void ReturnToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);

        if (source.spatialBlend > 0.5f)
        {
            active3DSources.Remove(source);
            pool3D.Enqueue(source);
        }
        else
        {
            active2DSources.Remove(source);
            pool2D.Enqueue(source);
        }

        source.transform.position = Vector3.zero;
    }

    private void Update()
    {
        // Return finished 2D sources
        for (int i = active2DSources.Count - 1; i >= 0; i--)
        {
            if (!active2DSources[i].isPlaying)
                ReturnToPool(active2DSources[i]);
        }

        // Return finished 3D sources
        for (int i = active3DSources.Count - 1; i >= 0; i--)
        {
            if (!active3DSources[i].isPlaying)
                ReturnToPool(active3DSources[i]);
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
                soundMap[mapping.soundType] = mapping.soundData;
        }
    }

    // ===================================================================
    // PUBLIC PLAY METHODS
    // ===================================================================

    /// <summary>
    /// Play 3D sound at world position (with optional delay)
    /// </summary>
    public static void Play(SoundType type, Vector3 position, float delay = 0f)
    {
        if (Instance == null) return;
        if (delay > 0f)
            Instance.StartCoroutine(Instance.DelayedPlay(type, position, true, delay));
        else
            Instance.InternalPlay(type, position, true);
    }

    /// <summary>
    /// Play 2D sound (UI, effects) with optional delay
    /// </summary>
    public static void Play2D(SoundType type, float delay = 0f)
    {
        if (Instance == null) return;
        if (delay > 0f)
            Instance.StartCoroutine(Instance.DelayedPlay(type, Vector3.zero, false, delay));
        else
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
        if (clip == null || (Instance.musicSource.clip == clip && Instance.musicSource.isPlaying)) return;
        
        Instance.musicSource.Stop();
        Instance.musicSource.clip = clip;
        Instance.musicSource.volume = data.GetRandomVolume();
        Instance.musicSource.pitch = data.GetRandomPitch();
        Instance.musicSource.loop = true;
        Instance.musicSource.Play();
    }
    static public void StopMusic()
    {
        if (Instance == null || Instance.musicSource == null) return;
        Instance.musicSource.Stop();
    }

    // ===================================================================

    private void InternalPlay(SoundType type, Vector3 worldPos, bool is3D)
    {
        if (!soundMap.TryGetValue(type, out SoundData soundData) || soundData == null)
        {
            Debug.LogWarning($"[SoundManager] No SoundData for: {type}");
            return;
        }

        var clip = soundData.GetNextClip();
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] No clip in: {soundData.name}");
            return;
        }

        var source = GetSource(is3D);

        if (is3D)
            source.transform.position = worldPos;

        source.clip = clip;
        source.volume = soundData.GetRandomVolume();
        source.pitch = soundData.GetRandomPitch();
        source.loop = false;
        source.Play();
    }

    private IEnumerator DelayedPlay(SoundType type, Vector3 pos, bool is3D, float delay)
    {
        yield return new WaitForSeconds(delay);
        InternalPlay(type, pos, is3D);
    }

    public void RebuildMap() => BuildSoundMap();
}