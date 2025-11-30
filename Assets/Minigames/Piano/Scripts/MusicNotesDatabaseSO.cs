using UnityEngine;

[CreateAssetMenu(fileName = "MusicNotesDatabaseSO", menuName = "Minigames/MusicNotesDatabaseSO", order = 0)]
public class MusicNotesDatabaseSO : ScriptableObject
{
    [SerializeField] private AudioClip[] _noteAudioClips; // Array to hold audio clips for each note
    public AudioClip GetAudioClipForNote(int noteID)
    {
        if (noteID >= 0 && noteID < _noteAudioClips.Length)
        {
            return _noteAudioClips[noteID];
        }
        else
        {
            Debug.LogWarning($"Note ID {noteID} is out of range.");
            return null;
        }
    }
}