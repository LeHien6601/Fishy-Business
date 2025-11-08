using UnityEngine;
using UnityEngine.UI;

public class UIInGame : UIView
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _borderImage;
    [SerializeField] private Sprite _dogSprite;
    [SerializeField] private Sprite _catSprite;
    [SerializeField] private Color _dogBorderColor;
    [SerializeField] private Color _catBorderColor;
    public override void Show()
    {
        bool isCat = GameplayManager.Instance.GetPlayerRole() == PlayerRole.Cat;
        _iconImage.sprite = isCat ? _catSprite : _dogSprite;
        _borderImage.color = isCat ? _catBorderColor : _dogBorderColor;
        base.Show();
    }
}
