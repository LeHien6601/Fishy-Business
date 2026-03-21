using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
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
    [SerializeField] private RectTransform _counterRect;
    [SerializeField] private Image _clockFill;
    [SerializeField] private RectTransform _myTurnRect;
    [SerializeField] private TextMeshProUGUI _counterTMP;
    [SerializeField] private RectTransform _phaseRect;
    [SerializeField] private TextMeshProUGUI _phaseTMP;
    [SerializeField] private RectTransform _turnRect;
    [SerializeField] private TextMeshProUGUI _turnTMP;
    [SerializeField] private UIToolTip _roleTooltip;
    [SerializeField] private TextMeshProUGUI _gameModeTMP;
    [SerializeField] private UIToolTip _gameModeTooltip;


    [Header("Properties")]
    [SerializeField] private Color _dogBorderColor;
    [SerializeField] private Color _catBorderColor;

    [Header("Symbol ShowUI")]
    [SerializeField] private Image _item1;
    [SerializeField] private Image _item2;
    [SerializeField] private SymbolSpriteSO _symbolSpriteSO;

    private bool _isMyTurn = false;
    private float _timer = 0;
    private bool _showTurn = false;
    private bool _resetTimer = false;

    private GameData _gameData;

    public override void Show()
    {
        string json = LobbyManager.Instance.currentLobby.Data[Constant.KEY_GAME_MODE_DATA].Value;
        Utils.UpdateGameModeData(json);
        _gameData = JsonUtility.FromJson<GameData>(json);
        bool isCat = GameplayManager.Instance.GetPlayerRole() == PlayerRole.Cat;
        _showTurn = false;
        _iconImage.sprite = isCat ? _catSprite : _dogSprite;
        _borderImage.color = isCat ? _catBorderColor : _dogBorderColor;
        UpdateGameMode();
        HideRect(_phaseRect);
        HideRect(_turnRect);
        _roleTooltip.SetText(isCat ? Constant.CAT_ROLE_DESCRIPTION : Constant.DOG_ROLE_DESCRIPTION);
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
        // GameplayManager.Instance.OnUseActionCard += HandleUIActionCard;
        GameplayManager.Instance.OnStartPhase += HandleStartPhase;
        GameplayManager.Instance.OnEndPhase += HandleEndPhase;
        _myTurnRect.localScale = Vector3.zero;
        _counterRect.localScale = Vector3.zero;

        _item1.gameObject.SetActive(false);
        _item2.gameObject.SetActive(false);
    }
    void OnDisable()
    {
        GameplayManager.Instance.OnStartedNewTurn -= HandleNewTurn;
        GameplayManager.Instance.OnUseActionCard -= HandleUIActionCard;
        GameplayManager.Instance.OnStartPhase -= HandleStartPhase;
        GameplayManager.Instance.OnEndPhase -= HandleEndPhase;
    }
    private async void HandleNewTurn(GameplayManager.StartedNewTurnEventArgs args)
    {
        if (args.ClientId == NetworkManager.Singleton.LocalClientId)
        {
            _isMyTurn = true;
            ShowRect(_myTurnRect);
        }
        else if (_isMyTurn)
        {
            _isMyTurn = false;
            HideRect(_myTurnRect);
        }
        ShowTurnText(args.TurnNumber);
        ShowRect(_counterRect);
        _resetTimer = true;
        await Task.Yield(); 
        StartCoroutine(TimerCountdown(_gameData.TurnInterval));
    }
    private async void HandleStartPhase(GameplayManager.StartPhaseEventArgs args)
    {
        Debug.Log("Start " + args.Phase);
        if (args.Phase != GamePhase.DayVoting)
            ShowPhaseText(GameMode.GetGamePhaseName(args.Phase));
        HideRect(_myTurnRect);
        _showTurn = true;
        if (args.Phase != GamePhase.Night)
        {
            _resetTimer = true;
            await Task.Yield();
            StartCoroutine(TimerCountdown(args.Duration));
            HideRect(_turnRect);
        }
    }
    private void HandleEndPhase(GamePhase phase)
    {
        if (phase == GamePhase.DayDiscussion) HideRect(_phaseRect);
    }
    private IEnumerator TimerCountdown(float duration)
    {
        _resetTimer = false;
        Debug.Log($"Start counter {duration}");
        _timer = duration;
        while (!_resetTimer)
        {
            if (_timer < 0) _timer = 0;
            _clockFill.fillAmount = 1 - _timer / duration;
            _counterTMP.text = Mathf.CeilToInt(_timer).ToString();
            _timer -= Time.deltaTime;
            yield return null;
        }
        Debug.Log($"End counter {duration}");
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
    private void UpdateGameMode()
    {
        bool isClassic = _gameData.GameModeIndex == 0;
        _gameModeTMP.text = isClassic ? Constant.CLASSIC_MODE : Constant.DAY_NIGHT_MODE;
        _gameModeTooltip.SetText(isClassic ? Constant.CLASSIC_MODE_DESCRIPTION : Constant.DAY_NIGHT_DESCRIPTION);
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
    private void ShowPhaseText(string phaseName)
    {
        _phaseRect.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        _phaseTMP.text = phaseName;
    }
    private void ShowTurnText(int turnNumber)
    {
        if (!_showTurn) return;
        _turnRect.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        _turnTMP.text = $"Turn {turnNumber + 1}";
    }
    private void ShowRect(RectTransform rect)
    {
        rect.localScale = Vector2.zero;
        rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
    }
    private void HideRect(RectTransform rect)
    {
        rect.DOScale(0f, 0.2f).SetEase(Ease.InBack);
    }
}
