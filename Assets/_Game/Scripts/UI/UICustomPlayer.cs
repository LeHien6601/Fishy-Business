using UnityEngine;
using UnityEngine.UI;

public class UICustomPlayer : UIView
{
    [Header("References")]
    [SerializeField] private Button[] _hatButtons;
    [SerializeField] private Button[] _faceButtons;
    [SerializeField] private Button[] _eyeGlassesButtons;
    [SerializeField] private Button[] _shirtButtons;
    [SerializeField] private Button[] _glovesButtons;
    [SerializeField] private Button[] _pantsButtons;
    [SerializeField] private Button[] _shoesButtons;

    private OutfitSelector _outfitSelector;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        OutfitSelector[] selectors = FindObjectsByType<OutfitSelector>(FindObjectsSortMode.None);
        foreach (OutfitSelector selector in selectors)
        {
            if (selector.IsOwner)
            {
                _outfitSelector = selector;
                break;
            }
        }
        _hatButtons[0].onClick.AddListener(() => HandleClickButton(0, false));
        _hatButtons[1].onClick.AddListener(() => HandleClickButton(0, true));
        _faceButtons[0].onClick.AddListener(() => HandleClickButton(1, false));
        _faceButtons[1].onClick.AddListener(() => HandleClickButton(1, true));
        _eyeGlassesButtons[0].onClick.AddListener(() => HandleClickButton(2, false));
        _eyeGlassesButtons[1].onClick.AddListener(() => HandleClickButton(2, true));
        _shirtButtons[0].onClick.AddListener(() => HandleClickButton(3, false));
        _shirtButtons[1].onClick.AddListener(() => HandleClickButton(3, true));
        _glovesButtons[0].onClick.AddListener(() => HandleClickButton(4, false));
        _glovesButtons[1].onClick.AddListener(() => HandleClickButton(4, true));
        _pantsButtons[0].onClick.AddListener(() => HandleClickButton(5, false));
        _pantsButtons[1].onClick.AddListener(() => HandleClickButton(5, true));
        _shoesButtons[0].onClick.AddListener(() => HandleClickButton(6, false));
        _shoesButtons[1].onClick.AddListener(() => HandleClickButton(6, true));
    }
    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _hatButtons[0].onClick.RemoveAllListeners();
        _hatButtons[1].onClick.RemoveAllListeners();
        _faceButtons[0].onClick.RemoveAllListeners();
        _faceButtons[1].onClick.RemoveAllListeners();
        _eyeGlassesButtons[0].onClick.RemoveAllListeners();
        _eyeGlassesButtons[1].onClick.RemoveAllListeners();
        _shirtButtons[0].onClick.RemoveAllListeners();
        _shirtButtons[1].onClick.RemoveAllListeners();
        _glovesButtons[0].onClick.RemoveAllListeners();
        _glovesButtons[1].onClick.RemoveAllListeners();
        _pantsButtons[0].onClick.RemoveAllListeners();
        _pantsButtons[1].onClick.RemoveAllListeners();
        _shoesButtons[0].onClick.RemoveAllListeners();
        _shoesButtons[1].onClick.RemoveAllListeners();
    }
    private void HandleClickButton(int outfitId, bool next)
    {
        if (_outfitSelector == null) return;
        SoundManager.Play2D(SoundType.ButtonClick);
        _outfitSelector.OwnerChangeOutfit(outfitId, next);
    }
}
