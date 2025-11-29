using System;
using System.Collections;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIInGame : UIView
{
    [Header("References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _borderImage;
    [SerializeField] private Sprite _dogSprite;
    [SerializeField] private Sprite _catSprite;
    [SerializeField] private RectTransform _myTurnRect;
    [SerializeField] private RectTransform _clockRect;
    [SerializeField] private Image _clockFill;


    [Header("Properties")]
    [SerializeField] private Color _dogBorderColor;
    [SerializeField] private Color _catBorderColor;

    [Header("Symbol ShowUI")]
    [SerializeField] private Image _item1;
    [SerializeField] private Image _item2;
    [SerializeField] private SymbolSpriteSO _symbolSpriteSO;

    private bool _isMyTurn = false;
    private float _timer = 0;

    public override void Show()
    {
        bool isCat = GameplayManager.Instance.GetPlayerRole() == PlayerRole.Cat;
        _iconImage.sprite = isCat ? _catSprite : _dogSprite;
        _borderImage.color = isCat ? _catBorderColor : _dogBorderColor;
        base.Show();
    }
    public override void Hide()
    {
        _isMyTurn = false;
        base.Hide();
    }
    void OnEnable()
    {
        GameplayManager.Instance.OnStartedNewTurn += HandleNewTurn;
        GameplayManager.Instance.OnUseActionCard += HandleUIActionCard;
        _myTurnRect.localScale = Vector3.zero;
        _clockRect.localScale = Vector3.zero;

        _item1.gameObject.SetActive(false);
        _item2.gameObject.SetActive(false);
    }
    void OnDisable()
    {
        GameplayManager.Instance.OnStartedNewTurn -= HandleNewTurn;
        GameplayManager.Instance.OnStartedNewTurn -= HandleNewTurn;
    }
    private void HandleNewTurn(GameplayManager.StartedNewTurnEventArgs args)
    {
        if (args.ClientId == NetworkManager.Singleton.LocalClientId)
        {
            _isMyTurn = true;
            _myTurnRect.localScale = Vector3.zero;
            _myTurnRect.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            _clockRect.localScale = Vector3.zero;
            _clockRect.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            StartCoroutine(TimerCoroutine());
        }
        else if (_isMyTurn)
        {
            _isMyTurn = false;
            _myTurnRect.localScale = Vector2.one;
            _myTurnRect.DOScale(0f, 0.2f).SetEase(Ease.InBack);
            _clockRect.localScale = Vector2.one;
            _clockRect.DOScale(0f, 0.2f).SetEase(Ease.InBack);
        }
    }
    private IEnumerator TimerCoroutine()
    {
        _timer = Constant.TURN_INTERVAL;
        while (_isMyTurn)
        {
            _timer -= Time.deltaTime;
            if (_timer < 0) _timer = 0;
            _clockFill.fillAmount = 1 - _timer / Constant.TURN_INTERVAL;
            yield return null;
        }
    }


    private void HandleUIActionCard(ActionCardType actionType, ToolType toolType)
    {
        if (actionType == ActionCardType.None) return;
        if (actionType == ActionCardType.Bomb)
        {
            ShowSymbol(_symbolSpriteSO.Bomb);
        }
        else if (actionType == ActionCardType.CheckGold)
        {
            ShowSymbol(_symbolSpriteSO.Map);
        }
        else if (actionType == ActionCardType.BrokenTool)
        {
            switch (toolType)
            {
                case ToolType.Cart:
                    ShowSymbol(_symbolSpriteSO.BreakCart);
                    break;
                case ToolType.Hat:
                    ShowSymbol(_symbolSpriteSO.BreakHat);
                    break;
                case ToolType.Shovel:
                    ShowSymbol(_symbolSpriteSO.BreakShovel);
                    break;
                default:
                    break;
            }
        }
        else if (actionType == ActionCardType.FixTool)
        {
            switch (toolType)
            {
                case ToolType.Cart:
                    ShowSymbol(_symbolSpriteSO.FixCart);
                    break;
                case ToolType.Hat:
                    ShowSymbol(_symbolSpriteSO.FixHat);
                    break;
                case ToolType.Shovel:
                    ShowSymbol(_symbolSpriteSO.FixShovel);
                    break;
                default:
                    break;
            }
        }
    }
    private void ShowSymbol(Sprite sprite)
    {
        Color color = Color.white;
        Sequence sequence = DOTween.Sequence();
        if (_item1.gameObject.activeInHierarchy)
        {
            _item2.sprite = sprite;
            _item2.gameObject.SetActive(true);
            color.a = 0f;
            _item2.color = color;
            
            sequence.Append(_item2.DOFade(1f, 1f));
            sequence.Append(_item2.DOFade(0f, 0.5f));
            sequence.OnComplete(() =>
            {
                _item2.gameObject.SetActive(false);
            });
            
        }
        else
        {
            _item1.sprite = sprite;
            _item1.gameObject.SetActive(true);
            color.a = 0f;
            _item1.color = color;
            sequence.Append(_item1.DOFade(1f, 1f));
            sequence.Append(_item1.DOFade(0f, 0.5f));
            sequence.OnComplete(() =>
            {
                _item1.gameObject.SetActive(false);
            });
        }
    }
}
