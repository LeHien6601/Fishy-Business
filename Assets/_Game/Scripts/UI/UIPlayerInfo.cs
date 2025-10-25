using System.Collections.Generic;
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
    [SerializeField] private string _tooShortNameNote = "Name is too short! Min length is 3 characters.";
    [SerializeField] private string _tooLongNameNote = "Name is too long! Max length is 12 characters.";
    [SerializeField] private string _saveInfoSuccessNote = "Player info saved successfully.";
    private const int MIN_NAME_LENGTH = 3;
    private const int MAX_NAME_LENGTH = 12;
    private int _selectedIconId = 0;
    private bool _isChanged = false;
    void OnEnable()
    {
        UpdateUI();
        LobbyManager.Instance.OnUpdatedCurrentLobby += HandleUpdateLobby;
        _backButton.onClick.AddListener(HandleClickBack);
    }
    void OnDisable()
    {
        LobbyManager.Instance.OnUpdatedCurrentLobby -= HandleUpdateLobby;
        _backButton.onClick.RemoveListener(HandleClickBack);
    }
    private void HandleUpdateLobby(LobbyManager.UpdateCurrentLobbyEventArgs args)
    {
        UpdateUI();
    }
    private void UpdateUI()
    {
        
    }
    private void HandleClickBack()
    {
        Hide();
        UIManager.Instance.ShowUI(EUIState.LobbyGameplay);
    }
    private void HandleClickSave()
    {
        
    }
}
