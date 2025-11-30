using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class InstrumentInput : MonoBehaviour
{
    [SerializeField] private bool _useUIButtons = false;
    [SerializeField] private Transform _buttonContainer;
    [SerializeField] private GameObject _buttonPrefab;
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

    private List<Transform> _buttons;

    void OnEnable()
    {
        if (_useUIButtons && _buttons == null)
        {
            _buttons = new List<Transform>();
            for (int i = 0; i < 21; i++)
            {
                GameObject buttonObj = Instantiate(_buttonPrefab, _buttonContainer);
                buttonObj.name = $"NoteButton_{i}";
                _buttons.Add(buttonObj.transform);
            }
        }
        _buttonContainer.transform.localScale = Vector3.zero;
        _buttonContainer.transform.DOScale(1f, 1f).SetEase(Ease.OutBack);
    }

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
                if (_useUIButtons)
                {
                    _buttons[i].localScale = Vector3.one;
                    _buttons[i].DOScale(0.8f, 0.05f).SetLoops(2, LoopType.Yoyo);
                }
            }
        }
    }
}