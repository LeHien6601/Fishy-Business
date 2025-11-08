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
    private bool _isMyTurn = false;
    private float _timer = 0;

    public override void Show()
    {
        bool isCat = GameplayManager.Instance.GetPlayerRole() == PlayerRole.Cat;
        _iconImage.sprite = isCat ? _catSprite : _dogSprite;
        _borderImage.color = isCat ? _catBorderColor : _dogBorderColor;
        base.Show();
    }
    void OnEnable()
    {
        GameplayManager.Instance.OnStartedNewTurn += HandleNewTurn;
        _myTurnRect.localScale = Vector3.zero;
        _clockRect.localScale = Vector3.zero;
    }
    void OnDisable()
    {
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
}
