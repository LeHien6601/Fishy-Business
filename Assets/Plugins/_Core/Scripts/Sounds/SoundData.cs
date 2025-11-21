using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New SoundData", menuName = "Audio/SoundData")]
public class SoundData : ScriptableObject
{
    public List<AudioClip> clips = new();

    [Header("Volume (dB)")]
    [Range(-40f, 40f)] public float minVolumeDB = -12f;
    [Range(-40f, 40f)] public float maxVolumeDB = -3f;

    [Header("Pitch")]
    [Range(0f, 2f)] public float minPitch = 0.8f;
    [Range(0f, 2f)] public float maxPitch = 1.2f;

    public enum PlaybackMode { Sequential, Random }
    public PlaybackMode playbackMode = PlaybackMode.Random;
    public string soundGroup = "Uncategorized";
    public string soundType = "Default";

    // Runtime helper (not shown in inspector)
    [HideInInspector] public int sequenceIndex = 0;

    // Linear getters (used at runtime)
    public float GetRandomVolume() => Random.Range(DBToLinear(minVolumeDB), DBToLinear(maxVolumeDB));
    public float GetRandomPitch() => Random.Range(minPitch, maxPitch);

    public AudioClip GetNextClip()
    {
        if (clips == null || clips.Count == 0) return null;
        sequenceIndex %= clips.Count;

        AudioClip clip;
        if (playbackMode == PlaybackMode.Sequential)
        {
            clip = clips[sequenceIndex];
            sequenceIndex = (sequenceIndex + 1) % clips.Count;
        }
        else
        {
            sequenceIndex = Random.Range(0, clips.Count);
            clip = clips[sequenceIndex];
        }
        return clip;
    }

    public static float DBToLinear(float dB) => Mathf.Clamp01(Mathf.Pow(10.0f, dB / 20.0f));
    public static float LinearToDB(float linear) => linear <= 0f ? -40f : Mathf.Clamp(20f * Mathf.Log10(linear), -40f, 40f);
}