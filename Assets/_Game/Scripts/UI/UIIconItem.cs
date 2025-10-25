using UnityEngine;
using UnityEngine.UI;

public class UIIconItem : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _borderImage;
    private int _iconId;
    public int IconId => _iconId;
    public void SetIcon(int iconId)
    {
        _iconId = iconId;
        Sprite iconSprite = GameConfig.Instance.GetPlayerIconById(iconId);
        if (iconSprite != null)
        {
            _iconImage.sprite = iconSprite;
        }
        else
        {
            Debug.LogWarning($"Icon sprite for ID {_iconId} not found.");
        }
    }
    public void SetSelected(bool isSelected)
    {
        // Implement visual changes for selection state
        _borderImage.color = isSelected ? Color.yellow : Color.white;
    }

    public Button GetButton()
    {
        return _button;
    }
}
