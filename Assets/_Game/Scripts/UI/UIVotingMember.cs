using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIVotingMember : MonoBehaviour
{
    #region Properties
    [Header("References")]
    [SerializeField] private Image _avaImg;
    [SerializeField] private TextMeshProUGUI _nameTMP;
    [SerializeField] private TextMeshProUGUI _emptyTMP;
    [SerializeField] private RectTransform _mineRect;
    [SerializeField] private RectTransform _dataRect;
    [SerializeField] private RectTransform _voteRect;
    [SerializeField] private RectTransform _voteCountRect;
    [SerializeField] private RectTransform _avaContainerRect;
    [SerializeField] private TextMeshProUGUI _voteTMP;
    [SerializeField] private TextMeshProUGUI _voteCountTMP;
    [SerializeField] private Button _yesBTN;
    [SerializeField] private Button _noBTN;
    [SerializeField] private Button _voteBTN;

    private UIInGameVoting _uiInGameVoting;
    private List<string> _votedMembers = new();
    private bool _isMine = false;
    private string _id;
    private bool _locked = false;
    #endregion

    #region Cycle
    void OnEnable()
    {
        _yesBTN.onClick.AddListener(HandleClickYes);
        _noBTN.onClick.AddListener(HandleClickNo);
        _voteBTN.onClick.AddListener(HandleClickKickButton);
    }
    void OnDisable()
    {
        _yesBTN.onClick.RemoveListener(HandleClickYes);
        _noBTN.onClick.RemoveListener(HandleClickNo);
        _voteBTN.onClick.RemoveListener(HandleClickKickButton);
    }
    #endregion

    #region Behaviors
    public void SetUIInGameVoting(UIInGameVoting uiInGameVoting)
    {
        _uiInGameVoting = uiInGameVoting;
    }
    public void SetMemberData(bool isMine, string name, Sprite ava, string id)
    {
        _isMine = isMine;
        _nameTMP.text = name;
        _avaImg.sprite = (ava != null) ? ava : _avaImg.sprite;
        _id = id;

        _dataRect.gameObject.SetActive(true);
        _voteRect.gameObject.SetActive(false);
        
        _emptyTMP.gameObject.SetActive(false);
        _nameTMP.gameObject.SetActive(true);
        _avaContainerRect.gameObject.SetActive(true);
        _mineRect.gameObject.SetActive(_isMine);

        _voteBTN.gameObject.SetActive(true);
        _voteCountRect.gameObject.SetActive(true);
        _voteCountTMP.text = _votedMembers.Count.ToString();
    }
    public void ResetMemberData()
    {
        _dataRect.gameObject.SetActive(true);
        _voteRect.gameObject.SetActive(false);
        _emptyTMP.gameObject.SetActive(true);
        _nameTMP.gameObject.SetActive(false);
        _avaContainerRect.gameObject.SetActive(false);
        _mineRect.gameObject.SetActive(false);
        _voteBTN.gameObject.SetActive(false);
        _voteCountRect.gameObject.SetActive(false);
        _votedMembers.Clear();
    }

    public void TakeVote(string voteId)
    {
        _votedMembers.Add(voteId);
        _voteCountTMP.text = _votedMembers.Count.ToString();
    }

    public void SetLock(bool isLocked)
    {
        _locked = isLocked;
        _voteBTN.gameObject.SetActive(!isLocked);
    }

    public void HandleClickKickButton()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        ToggleVotingContainer();
    }

    private void ToggleVotingContainer()
    {
        _dataRect.gameObject.SetActive(!_dataRect.gameObject.activeSelf);
        _voteRect.gameObject.SetActive(!_voteRect.gameObject.activeSelf);
        _voteTMP.text = $"Vote {_nameTMP.text}?";
    }
    private void HandleClickYes()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        if (!_locked)
            _uiInGameVoting.TriggerVoting(_id);
        ToggleVotingContainer();
    }
    private void HandleClickNo()
    {
        SoundManager.Play2D(SoundType.ButtonClick);
        ToggleVotingContainer();
    }
    
    #endregion
}
