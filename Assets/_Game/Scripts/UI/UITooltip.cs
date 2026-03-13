using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [Tooltip("The actual UI element (panel/text) that should appear on hover")]
    [SerializeField] private GameObject _tooltipVisual;
    [SerializeField] private TextMeshProUGUI _tooltipTMP;
    [Header("Properties")]
    [SerializeField] private float _triggerTime = 0.2f;
    [SerializeField] private bool _defaultHide = true;
    private float _timer;
    private bool _isHovered = false;

    private void Start()
    {
        _tooltipVisual.SetActive(!_defaultHide);
    }
    void Update()
    {
        if (!_isHovered) return;
        if (Time.time - _timer > _triggerTime)
        {
            _tooltipVisual.SetActive(true);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        _timer = Time.time;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _tooltipVisual.SetActive(false);
    }
    public void SetText(string text)
    {
        if (_tooltipTMP == null) return;
        _tooltipTMP.text = text;
    }
}