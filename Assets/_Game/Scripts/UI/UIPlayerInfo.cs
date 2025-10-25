using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerInfo : UIView
{
    [Header("References")]
    [SerializeField] private TMP_InputField _nameInputField;
    [SerializeField] private TextMeshProUGUI _noteTMP;
    [SerializeField] private UIIconItem _iconItem;
    [SerializeField] private List<UIIconItem> _iconItems = new();
    [SerializeField] private UIIconItem _prefabIconItem;
    [SerializeField] private Transform _iconItemContainer;
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _saveButton;

    [Header("Properties")]
    [SerializeField] private string _tooShortNameNote = "Name is too short!\nMin length is 3 characters.";
    [SerializeField] private string _tooLongNameNote = "Name is too long!\nMax length is 12 characters.";
    [SerializeField] private string _saveInfoSuccessNote = "Player info saved successfully.";
    private const int MIN_NAME_LENGTH = 3;
    private const int MAX_NAME_LENGTH = 12;
    private int _selectedIconId = 0;
    private bool _hasChangedName = false;
    private bool _hasChangeIcon = false;
    void OnEnable()
    {
        UpdateUI();
        _backButton.onClick.AddListener(HandleClickBack);
        _saveButton.onClick.AddListener(HandleClickSave);
        _nameInputField.onValueChanged.AddListener(delegate { HandleChangeInputName(); });
    }
    void OnDisable()
    {
        _backButton.onClick.RemoveListener(HandleClickBack);
        _saveButton.onClick.RemoveListener(HandleClickSave);
        _nameInputField.onValueChanged.RemoveListener(delegate { HandleChangeInputName(); });
    }
    private void UpdateUI()
    {
        _nameInputField.text = PlayerInfoManager.Instance.PlayerName;
        _selectedIconId = PlayerInfoManager.Instance.PlayerIconId;
        _iconItem.SetIcon(_selectedIconId);
        if (_iconItems.Count == 0)
        {
            for (int i = 0; i < GameConfig.Instance.playerIcons.Count; i++)
            {
                UIIconItem newIconItem = Instantiate(_prefabIconItem, _iconItemContainer);
                newIconItem.SetIcon(i);
                int iconId = i;
                newIconItem.GetButton().onClick.AddListener(() => HandleSelectIcon(iconId));
                _iconItems.Add(newIconItem);
                newIconItem.SetSelected(i == _selectedIconId);
            }
        }
        _saveButton.interactable = false;
        _hasChangedName = false;
        _hasChangeIcon = false;
        _noteTMP.gameObject.SetActive(false);
    }
    private void HandleChangeInputName()
    {
        _hasChangedName = _nameInputField.text.Trim() != PlayerInfoManager.Instance.PlayerName;
        _saveButton.interactable = _hasChangedName || _hasChangeIcon;
    }
    private void HandleSelectIcon(int iconId)
    {
        _selectedIconId = iconId;
        _hasChangeIcon = _selectedIconId != PlayerInfoManager.Instance.PlayerIconId;
        _saveButton.interactable = _hasChangedName || _hasChangeIcon;
        foreach (var item in _iconItems)
        {
            item.SetSelected(item.IconId == iconId);
        }
    }
    private void HandleClickBack()
    {
        Hide();
    }
    private void HandleClickSave()
    {
        string newName = _nameInputField.text.Trim();
        if (newName.Length < MIN_NAME_LENGTH)
        {
            ShowNote(_tooShortNameNote);
            return;
        }
        if (newName.Length > MAX_NAME_LENGTH)
        {
            ShowNote(_tooLongNameNote);
            return;
        }
        PlayerInfoManager.Instance.UpdatePlayerInfo(newName, _selectedIconId);
        ShowNote(_saveInfoSuccessNote);
        _iconItem.SetIcon(_selectedIconId);
        _hasChangedName = false;
        _hasChangeIcon = false;
        _saveButton.interactable = false;
    }
    private async void ShowNote(string note)
    {
        _noteTMP.text = note;
        _noteTMP.gameObject.SetActive(true);
        _noteTMP.rectTransform.localScale = Vector3.zero;
        _noteTMP.rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        await Task.Delay(2000);
        _noteTMP.rectTransform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            _noteTMP.gameObject.SetActive(false);
        });
    }
}
