using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Emoter : MonoBehaviour
{
    [SerializeField] private Image[] _placeholders;
    // [SerializeField] private List<EmoteSO> _emotes;
    [SerializeField] private InputReaderSO _inputReader;
    [SerializeField] private GameObject _emoteWheel;
    private int _currentIndex;
    private bool _selected = false; //will be false if there s no wheel crolls after opening emote wheel

    public static event System.Action<int> OnEmoteSelected; // int is the index of the selected emote in the wheel, starts from 0
    private void Awake()
    {
        _emoteWheel.SetActive(false);
        Setup();
    }

    private void Setup()
    {
        _currentIndex = 0;
    }

    private void OnEnable()
    {
        _inputReader.OpenEmoteEvent += OpenEmoteWheel;
        _inputReader.CloseEmoteEvent += CloseEmoteWheel;
    }

    private void OpenEmoteWheel()
    {
        _selected = false;
        _emoteWheel.SetActive(true);
        _inputReader.ScrollWheelEvent += ScrollThroughEmotes; // only listen to scroll wheel event after opening the wheel
    }

    private void CloseEmoteWheel()
    {
        UnHighlightEmote(_currentIndex);
        _emoteWheel.SetActive(false);
        _inputReader.ScrollWheelEvent -= ScrollThroughEmotes;
        if (_selected)
        {
            OnEmoteSelected?.Invoke(_currentIndex);
        }
    }

    private void ScrollThroughEmotes(float input)
    {
        if (input == 0.0f)
            return;

        if (_selected)
        {
            UnHighlightEmote(_currentIndex);
            HighlightEmote(NextCircleIndex(input < 0));
            // NextCircleIndex(input < 0);
        }
        else
        {
            // If it is the first wheel scroll, then hight light the current emote instead of the next one
            _selected = true;
            HighlightEmote(_currentIndex);
        }
    }

    private void HighlightEmote(int index)
    {
        _placeholders[index].transform.DOScale(2.0f, 0.1f);
    }

    private void UnHighlightEmote(int index)
    {
        _placeholders[index].transform.DOScale(1f, 0.1f);
    }

    private int NextCircleIndex(bool clockwise)
    {
        if (clockwise)
        {
            _currentIndex = _currentIndex >= _placeholders.Length - 1 ? 0 : _currentIndex + 1;
            return _currentIndex;
        }
        else
        {
            _currentIndex = _currentIndex <= 0 ? _placeholders.Length - 1 : _currentIndex - 1;
            return _currentIndex;
        }
    }

    private void OnDisable()
    {
        _inputReader.OpenEmoteEvent -= OpenEmoteWheel;
        _inputReader.CloseEmoteEvent -= CloseEmoteWheel;
    }
}