using UnityEngine;
using UnityEngine.UI;

public class UIIconItem : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _iconImage;
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
}
