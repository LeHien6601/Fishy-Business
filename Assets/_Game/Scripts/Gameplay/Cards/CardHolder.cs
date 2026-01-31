using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.XR;

public class CardHolder : MonoBehaviour
{
    [Header("Card Layout Settings")]
    public static int MaxHandSize = 7;
    public static float ArcAngle = 30f;
    public static float ArcRadius = 1.8f;
    public static float AnimationDuration = 0.3f;
    public const float AddCardDuration = 0.6f;
    public static float CardThickness = 0.01f;
    [SerializeField] private Quaternion rotationOffset = Quaternion.identity;
    [SerializeField] private Transform _beforeFaceSlot;
    [SerializeField] private Vector3 _handOffset;
    [SerializeField] private Transform _cardContainer;
    [SerializeField] private Vector3 _othersCoinContainerPosition;
    [SerializeField] private Vector3 _myCoinContainerPosition;
    [SerializeField] private Transform _coinContainer;

    [Header("VFX")]
    [SerializeField] private ParticleSystem _repairCoinVFX;
    [SerializeField] private ParticleSystem _destroyCoinVFX;

    private readonly List<Card> handCards = new();
    public int CardCount => handCards.Count;
    // hovering 1 card at a time
    private Card _hoveringCard = null;
    private bool _isMine = false;
    public bool IsMine
    {
        get { return _isMine; }
        set
        {
            _isMine = value;
            HandleCoinContainerPosition();
        }
    }
    public bool IsTurn = false;

    private bool _cart = true;
    public bool Cart
    {
        get { return _cart; }
        set
        {
            _cart = value;
            // if (_cartCoin != null)
            //     _cartCoin.SetActive(value);
        }
    }

    private bool _nightVision = false;
    public bool NightVision
    {
        get { return _nightVision; }
        set
        {
            _nightVision = value;
        }
    }
    [SerializeField] private GameObject _cartCoin;
    private Vector3 _initScaleCoin;

    private bool _hat = true;
    public bool Hat
    {
        get { return _hat; }
        set
        {
            _hat = value;
            // if (_hatCoin != null)
            //     _hatCoin.SetActive(value);
        }
    }
    [SerializeField] private GameObject _hatCoin;

    private bool _shovel = true;
    public bool Shovel
    {
        get { return _shovel; }
        set
        {
            _shovel = value;
            // if (_shovelCoin != null)
            //     _shovelCoin.SetActive(value);
        }
    }
    [SerializeField] private GameObject _shovelCoin;
    public PlayerRole PlayerRole;

    void Awake()
    {
        _initScaleCoin = _hatCoin.transform.localScale;
    }

    #region CARDS

    /// <summary>
    /// Thêm card đã có sẵn (được spawn từ nơi khác)
    /// Using on Deal Cards or Draw Card
    /// </summary>
    public void AddCard(Card card)
    {
        if (card == null) return;

        handCards.Add(card);
        card.transform.SetParent(_cardContainer, true);
        card.transform.localScale = Vector3.one;
        card.Holder = this;
        card.Location = CardLocation.PlayerHand;

        card.transform.DOMove(_beforeFaceSlot.transform.position, AddCardDuration);
        card.transform.DORotateQuaternion(_beforeFaceSlot.transform.rotation, AddCardDuration);

        this.WaitThenExecute(AddCardDuration, () => UpdateCardPositions());
        SoundManager.Play2D(SoundType.DealCard);
    }

    /// <summary>
    /// Using when click right mouse on card
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public Card RemoveCard(Card card)
    {
        if (card == null) return null;

        handCards.Remove(card);
        card.Location = CardLocation.Discarded; // hoặc OnBoard nếu chơi ra bàn
        UpdateCardPositions();
        SoundManager.Play2D(SoundType.DealCard);
        return card;
    }

    public Card RemoveCard(Card card, CardLocation cardLocation)
    {
        if (card == null) return null;

        handCards.Remove(card);
        card.Location = cardLocation;
        UpdateCardPositions();
        SoundManager.Play2D(SoundType.DealCard);
        return card;
    }

    public Card RemoveCard(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= handCards.Count) return null;
        return RemoveCard(handCards[cardIndex]);
    }

    public Card RemoveRandomCard()
    {
        if (handCards.Count == 0) return null;
        int randomIndex = Random.Range(0, handCards.Count);
        return RemoveCard(handCards[randomIndex]);
    }

    /// <summary>
    /// Invoke when click left mouse on Card
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public Card UseCard(Card card)
    {
        // TODO: Move Card to Board Game
        // Remove from hand and hand will not mark it OnBoard yet.
        // BoardManager sẽ tiếp nhận object và quản lý tiếp (place / cancel)
        return RemoveCard(card, CardLocation.None);
    }

    public void DiscardRandomCard()
    {
        if (handCards.Count == 0)
            return;
        int random = Random.Range(0, handCards.Count);
        if (handCards[random] == null) return;
        handCards[random].TriggerDiscardCard();
    }

    public int GetCardIndex(Card card)
    {
        return handCards.IndexOf(card);
    }

    public List<Card> RemoveCards(List<int> indexes)
    {
        List<Card> cards = new();
        foreach (int i in indexes)
        {
            if (i >= 0 && i < handCards.Count)
                cards.Add(handCards[i]);
        }

        handCards.RemoveAll(c => cards.Contains(c));
        UpdateCardPositions();
        return cards;
    }

    public void SelectCard(Card card)
    {
        if (handCards.Contains(card) == false) return;
        if (_hoveringCard)
            return;
        _hoveringCard = card;
        Transform t = card.transform;
        Vector3 endPos = t.position + t.up * 0.1f;
        t.DOMove(endPos, AnimationDuration).SetEase(Ease.OutBack);
        card.Highlight(true); //yellow highlight

        SoundManager.Play2D(SoundType.DealCard);
    }


    /// <summary>
    /// Bỏ chọn card, đưa về vị trí ban đầu
    /// </summary>
    public void UnSelectCard(int index)
    {
        if (index < 0 || index >= handCards.Count) return;
        var (endPos, endRot) = GetCardPositionAndRotation(index);
        handCards[index].transform.DOMove(endPos, AnimationDuration).SetEase(Ease.OutQuad);
        handCards[index].transform.DORotateQuaternion(endRot, AnimationDuration).SetEase(Ease.OutQuad);
        handCards[index].OffHighlight();
        _hoveringCard = null;
    }

    public void UnSelectCard(Card card)
    {
        int index = handCards.IndexOf(card);
        if (index < 0 || index >= handCards.Count) return;

        var (endPos, endRot) = GetCardPositionAndRotation(index);
        card.transform.DOMove(endPos, AnimationDuration).SetEase(Ease.OutQuad);
        card.transform.DORotateQuaternion(endRot, AnimationDuration).SetEase(Ease.OutQuad);
        card.OffHighlight();
        _hoveringCard = null;
    }

    /// <summary>
    /// Cập nhật vị trí + góc của toàn bộ card trong tay (fan hình cánh cung)
    /// </summary>
    private void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;

        float cardSpacing = ArcAngle / MaxHandSize;
        float firstCardAngle = (handCards.Count - 1) * cardSpacing / 2f;

        for (int i = 0; i < handCards.Count; i++)
        {
            float angle = firstCardAngle - i * cardSpacing;
            float angleRad = angle * Mathf.Deg2Rad;

            // Rotation fan
            Quaternion faceForward = Quaternion.LookRotation(transform.forward, transform.up);
            Quaternion tiltRotation = Quaternion.Euler(0f, 0f, -angle);
            Quaternion endRotation = faceForward * tiltRotation * rotationOffset;

            // Position fan
            Vector3 localPos = new(
                Mathf.Sin(angleRad) * ArcRadius,
                Mathf.Cos(angleRad) * ArcRadius - ArcRadius,
                0f
            );
            Vector3 offset = (i - handCards.Count / 2f) * CardThickness * transform.forward;
            Vector3 endPos = transform.TransformPoint(localPos) + offset + _handOffset;

            handCards[i].DOKill();
            // Animate with DOTween
            handCards[i].transform.DOMove(endPos, AnimationDuration).SetEase(Ease.OutQuad);
            handCards[i].transform.DORotateQuaternion(endRotation, AnimationDuration).SetEase(Ease.OutQuad);
        }
    }

    private (Vector3, Quaternion) GetCardPositionAndRotation(int index)
    {
        float cardSpacing = ArcAngle / MaxHandSize;
        float firstCardAngle = (handCards.Count - 1) * cardSpacing / 2f;
        float angle = firstCardAngle - index * cardSpacing;
        float angleRad = angle * Mathf.Deg2Rad;

        Quaternion faceForward = Quaternion.LookRotation(transform.forward, transform.up);
        Quaternion tiltRotation = Quaternion.Euler(0f, 0f, -angle);
        Quaternion endRotation = faceForward * tiltRotation * rotationOffset;

        Vector3 localPos = new(
            Mathf.Sin(angleRad) * ArcRadius,
            Mathf.Cos(angleRad) * ArcRadius - ArcRadius,
            0f
        );
        Vector3 offset = (index - handCards.Count / 2f) * CardThickness * transform.forward;
        Vector3 endPos = transform.TransformPoint(localPos) + offset + _handOffset;

        return (endPos, endRotation);
    }

    public bool IsEmpty() => handCards.Count == 0;

    public bool IsTheSelectingCard(Card card) => card == _hoveringCard;
    public Transform BeforeFaceSlot() => _beforeFaceSlot;
    #endregion

    #region TOOLS

    public bool HasTool(ToolType toolType)
    {
        return toolType switch
        {
            ToolType.Cart => Cart,
            ToolType.Hat => Hat,
            ToolType.Shovel => Shovel,
            ToolType.CartHat => Cart && Hat,
            ToolType.CartShovel => Cart && Shovel,
            ToolType.HatShovel => Hat && Shovel,
            _ => false,
        };
    }
    public void SetTool(ToolType toolType, bool isRepair)
    {
        SoundManager.Play2D(isRepair ? SoundType.CoinAppear : SoundType.CoinDisappear);
        switch (toolType)
        {
            case ToolType.Cart:
                if (Cart != isRepair)
                {
                    AnimationForTool(_cartCoin, isRepair);
                    GameplayManager.Instance.TriggerActionCard(isRepair ?
                                ActionCardType.FixTool : ActionCardType.BrokenTool, ToolType.Cart);
                }
                Cart = isRepair;
                break;
            case ToolType.Hat:
                if (Hat != isRepair)
                {
                    AnimationForTool(_hatCoin, isRepair);
                    GameplayManager.Instance.TriggerActionCard(isRepair ?
                                ActionCardType.FixTool : ActionCardType.BrokenTool, ToolType.Hat);
                }
                Hat = isRepair;
                break;
            case ToolType.Shovel:
                if (Shovel != isRepair)
                {
                    AnimationForTool(_shovelCoin, isRepair);
                    GameplayManager.Instance.TriggerActionCard(isRepair ?
                                ActionCardType.FixTool : ActionCardType.BrokenTool, ToolType.Shovel);
                }
                Shovel = isRepair;
                break;
            case ToolType.CartHat:
                if (Cart != isRepair)
                {
                    AnimationForTool(_cartCoin, isRepair);
                    GameplayManager.Instance.TriggerActionCard(isRepair ?
                                ActionCardType.FixTool : ActionCardType.BrokenTool, ToolType.Cart);
                }
                if (Hat != isRepair)
                {
                    AnimationForTool(_hatCoin, isRepair);
                    GameplayManager.Instance.TriggerActionCard(isRepair ?
                                ActionCardType.FixTool : ActionCardType.BrokenTool, ToolType.Hat);
                }
                Cart = Hat = isRepair;
                break;
            case ToolType.CartShovel:
                if (Cart != isRepair)
                {
                    AnimationForTool(_cartCoin, isRepair);
                    GameplayManager.Instance.TriggerActionCard(isRepair ?
                                ActionCardType.FixTool : ActionCardType.BrokenTool, ToolType.Cart);
                }
                if (Shovel != isRepair)
                {
                    AnimationForTool(_shovelCoin, isRepair);
                    GameplayManager.Instance.TriggerActionCard(isRepair ?
                                                    ActionCardType.FixTool : ActionCardType.BrokenTool, ToolType.Shovel);
                }
                Cart = Shovel = isRepair;
                break;
            case ToolType.HatShovel:
                if (Hat != isRepair)
                {
                    AnimationForTool(_hatCoin, isRepair);
                    GameplayManager.Instance.TriggerActionCard(isRepair? 
                                ActionCardType.FixTool : ActionCardType.BrokenTool, ToolType.Hat);  
                }
                if (Shovel != isRepair)
                {
                    AnimationForTool(_shovelCoin, isRepair);
                    GameplayManager.Instance.TriggerActionCard(isRepair? 
                                ActionCardType.FixTool : ActionCardType.BrokenTool, ToolType.Shovel);  
                }
                Hat = Shovel = isRepair;
                break;
        }
    }
    private void AnimationForTool(GameObject gameObject, bool isRepair)
    {
        if (gameObject == null) return;
        SpawnVFX(gameObject.transform, isRepair);
        Sequence sequence = DOTween.Sequence();
        if (isRepair)
        {
            gameObject.transform.localScale = Vector2.zero;
            gameObject.SetActive(true);
            sequence.Append(gameObject.transform.DOScale(_initScaleCoin * 2, 0.1f));
            sequence.Append(gameObject.transform.DOScale(_initScaleCoin, 0.4f));
        }
        else
        {
            gameObject.transform.localScale = _initScaleCoin;
            sequence.Append(gameObject.transform.DOScale(_initScaleCoin * 2, 0.1f));
            sequence.Append(gameObject.transform.DOScale(0f, 0.4f));
            sequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });

        }
    }
    private void SpawnVFX(Transform transform, bool isRepair)
    {
        if (transform == null) return;
        var vfx = Instantiate(isRepair ? _repairCoinVFX : _destroyCoinVFX);
        vfx.transform.position = transform.position;
        this.WaitThenExecute(2f, () =>
        {
            if (vfx != null || vfx.gameObject != null)
            {
                Destroy(vfx.gameObject);
            }
        });
    }

    public bool HasAllTools() => Cart && Hat && Shovel;
    public bool LacksATool() => !Cart || !Hat || !Shovel;
    #endregion

    #region COINS
    private void HandleCoinContainerPosition()
    {
        _coinContainer.localPosition = IsMine ? _myCoinContainerPosition : _othersCoinContainerPosition;
    }
    #endregion
    public void SetRole(PlayerRole playerRole)
    {
        PlayerRole = playerRole;
    }
}


public enum PlayerRole
{
    Unknown,
    Cat,
    Dog,
}