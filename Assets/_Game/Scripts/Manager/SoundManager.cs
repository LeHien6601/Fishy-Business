using UnityEngine;
using System.Collections.Generic;
using HHDCore;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : SingletonMono<SoundManager>
{

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    private Dictionary<SoundType, SoundData> soundMap;

    public override void Awake()
    {
        base.Awake();  
        BuildSoundMap();
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
            if (mapping.soundData != null)
            {
                if (!soundMap.ContainsKey(mapping.soundType))
                {
                    soundMap[mapping.soundType] = mapping.soundData;
                }
                else
                {
                    Debug.LogWarning($"Duplicate SoundType mapping: {mapping.soundType}");
                }
            }
            else
            {
                Debug.LogWarning($"SoundType {mapping.soundType} has no SoundData assigned!");
            }
        }
    }

    public static void Play(SoundType type, AudioSource overrideSource = null)
    {
        if (Instance == null) return;

        if (!Instance.soundMap.TryGetValue(type, out SoundData soundData) || soundData == null)
        {
            Debug.LogWarning($"No SoundData mapped for SoundType: {type}");
            return;
        }

        var clip = soundData.GetNextClip();
        if (clip == null)
        {
            Debug.LogWarning($"SoundData for {type} has no clips!");
            return;
        }

        var source = overrideSource != null ? overrideSource : Instance.sfxSource;

        source.pitch = soundData.GetRandomPitch();
        source.volume = soundData.GetRandomVolume();

        source.PlayOneShot(clip);
    }

    public static void PlayMusic(SoundType type)
    {
        if (Instance == null || Instance.musicSource == null) return;
        Play(type, Instance.musicSource);
        // Optional: loop music
        Instance.musicSource.loop = true;
    }

    // Optional: Rebuild map if config changes at runtime (rare)
    public void RebuildMap() => BuildSoundMap();
}