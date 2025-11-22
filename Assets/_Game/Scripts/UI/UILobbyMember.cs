using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UILobbyMember : MonoBehaviour, IPointerClickHandler
{
    #region Properties
    [Header("References")]
    [SerializeField] private Image _avaImg;
    [SerializeField] private TextMeshProUGUI _nameTMP;
    [SerializeField] private TextMeshProUGUI _emptyTMP;
    [SerializeField] private RectTransform _mineRect;
    [SerializeField] private RectTransform _soundRect;
    [SerializeField] private RectTransform _dataRect;
    [SerializeField] private RectTransform _kickRect;
    [SerializeField] private RectTransform _avaContainerRect;
    [SerializeField] private TextMeshProUGUI _kickTMP;
    [SerializeField] private Button _yesBTN;
    [SerializeField] private Button _noBTN;

    private bool _isMine = false;
    private string _id;
    #endregion

    #region Cycle
    void OnEnable()
    {
        _yesBTN.onClick.AddListener(HandleClickYes);
        _noBTN.onClick.AddListener(HandleClickNo);
    }
    void OnDisable()
    {
        _yesBTN.onClick.RemoveListener(HandleClickYes);
        _noBTN.onClick.RemoveListener(HandleClickNo);
    }
    #endregion

    #region Behaviors
    public void SetMemberData(bool isMine, string name, Sprite ava, string id)
    {
        _isMine = isMine;
        _nameTMP.text = name;
        _avaImg.sprite = (ava != null) ? ava : _avaImg.sprite;
        _id = id;

        _dataRect.gameObject.SetActive(true);
        _kickRect.gameObject.SetActive(false);
        
        _emptyTMP.gameObject.SetActive(false);
        _nameTMP.gameObject.SetActive(true);
        _avaContainerRect.gameObject.SetActive(true);
        _mineRect.gameObject.SetActive(_isMine);
        _soundRect.gameObject.SetActive(!_isMine);
    }
    public void ResetMemberData()
    {
        _dataRect.gameObject.SetActive(true);
        _kickRect.gameObject.SetActive(false);
        _emptyTMP.gameObject.SetActive(true);
        _nameTMP.gameObject.SetActive(false);
        _avaContainerRect.gameObject.SetActive(false);
        _mineRect.gameObject.SetActive(false);
        _soundRect.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        if (!NetworkManager.Singleton.IsHost) return;
        if (_isMine) return;
        if (_emptyTMP.gameObject.activeSelf) return;
        ToggleKickContainer();
    }

    private void ToggleKickContainer()
    {
        _dataRect.gameObject.SetActive(!_dataRect.gameObject.activeSelf);
        _kickRect.gameObject.SetActive(!_kickRect.gameObject.activeSelf);
        _kickTMP.text = $"Kick {_nameTMP.text}?";
    }
    private void HandleClickYes()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        LobbyManager.Instance.KickPlayerAsync(_id);
        ToggleKickContainer();
    }
    private void HandleClickNo()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        ToggleKickContainer();
    }
    #endregion
}
