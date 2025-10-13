using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILobbyMember : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _avaImg;
    [SerializeField] private TextMeshProUGUI _nameTMP;
    [SerializeField] private TextMeshProUGUI _emptyTMP;
    [SerializeField] private RectTransform _mineRect;
    [SerializeField] private RectTransform _soundRect;

    private bool _isMine = false;
    private string _id;
    public void SetMemberData(bool isMine, string name, Sprite ava, string id)
    {
        _isMine = isMine;
        _nameTMP.text = name;
        _avaImg.sprite = (ava != null) ? ava : _avaImg.sprite;
        _id = id;

        _emptyTMP.gameObject.SetActive(false);
        _nameTMP.gameObject.SetActive(true);
        _avaImg.gameObject.SetActive(true);
        _mineRect.gameObject.SetActive(_isMine);
        _soundRect.gameObject.SetActive(!_isMine);
    }
    public void ResetMemberData()
    {
        _emptyTMP.gameObject.SetActive(true);
        _nameTMP.gameObject.SetActive(false);
        _avaImg.gameObject.SetActive(false);
        _mineRect.gameObject.SetActive(false);
        _soundRect.gameObject.SetActive(false);
    }
}
