using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIValueSlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TextMeshProUGUI _valueTMP;

    [SerializeField] private float _minValue;
    [SerializeField] private float _maxValue;
    [SerializeField] private string _postFix = "s";

    void Awake()
    {
        _slider.minValue = _minValue;
        _slider.maxValue = _maxValue;
        _slider.wholeNumbers = true;
    }

    void OnEnable()
    {
        _slider.onValueChanged.AddListener(UpdateUI);
    }
    void OnDisable()
    {
        _slider.onValueChanged.RemoveListener(UpdateUI);
    }

    public void SetValue(float value)
    {
        _slider.value = value;
    }

    public float GetValue()
    {
        return _slider.value;
    }

    private void UpdateUI(float value)
    {
        _valueTMP.text = value.ToString() + _postFix;
    }
}
