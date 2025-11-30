using UnityEngine;
using UnityEngine.Events;

public class InstrumentInput : MonoBehaviour
{
    // A mapping of your 21 note indices (0-20) to Unity KeyCodes
    private readonly KeyCode[] NoteKeyMap = new KeyCode[]
    {
        // Notes 14-20 (Top Row: Q, W, E, R, T, Y, U)
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U,
        // Notes 7-13 (Middle Row: A, S, D, F, G, H, J)
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J,
        // Notes 0-6 (Bottom Row: Z, X, C, V, B, N, M)
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M
    };

    public event UnityAction<int> OnNotePlayed;

    /// <summary>
    /// Returns the KeyCode corresponding to the given note index (0-20).
    /// </summary>
    /// <param name="index">The note index (0-20).</param>
    /// <returns>The KeyCode mapped to the note index.</returns>
    private KeyCode GetKeyForNote(int index)
    {
        // We use a bounds check just in case an invalid index is passed.
        if (index >= 0 && index < NoteKeyMap.Length)
        {
            return NoteKeyMap[index];
        }

        // Return a default key if the index is out of bounds (e.g., Space or None)
        Debug.LogError($"Note index {index} is out of bounds (0-{NoteKeyMap.Length - 1}).");
        return KeyCode.None;
    }

    void Update()
    {
        // The loop is slightly more efficient if you iterate over the size of the mapping array.
        for (int i = 0; i < NoteKeyMap.Length; i++)
        {
            if (Input.GetKeyDown(GetKeyForNote(i)))
            {
                // Invoke the event with the note index (i)
                OnNotePlayed?.Invoke(i);
            }
        }
    }
}