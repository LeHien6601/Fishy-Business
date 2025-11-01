using System.Collections.Generic;
using DG.Tweening;
using Mono.Cecil.Cil;
using UnityEngine;

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

    private readonly List<Card> handCards = new();
    public int CardCount => handCards.Count;
    private Card _hoveringCard; // hovering 1 card at a time
    public bool IsMine = false;
    public bool IsTurn = false;
    public bool Cart = true;
    public bool Hat = true;
    public bool Shovel = true;


    /// <summary>
    /// Thêm card đã có sẵn (được spawn từ nơi khác)
    /// Using on Deal Cards or Draw Card
    /// </summary>
    public void AddCard(Card card)
    {
        if (card == null) return;

        handCards.Add(card);
        card.transform.SetParent(transform, true);
        card.Holder = this;
        card.Location = CardLocation.PlayerHand;

        card.transform.DOMove(_beforeFaceSlot.transform.position, AddCardDuration);
        card.transform.DORotateQuaternion(_beforeFaceSlot.transform.rotation, AddCardDuration);

        this.WaitThenExecute(AddCardDuration, () => UpdateCardPositions());
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
        return card;
    }

    public Card RemoveCard(Card card, CardLocation cardLocation)
    {
        if (card == null) return null;

        handCards.Remove(card);
        card.Location = cardLocation;
        UpdateCardPositions();
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
            Vector3 endPos = transform.TransformPoint(localPos) + offset;

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
        Vector3 endPos = transform.TransformPoint(localPos) + offset;

        return (endPos, endRotation);
    }

    public bool IsEmpty() => handCards.Count == 0;

    public Transform BeforeFaceSlot() => _beforeFaceSlot;

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
    public bool HasAllTools() => Cart && Hat && Shovel;
    public bool LacksATool() => !Cart || !Hat || !Shovel;
    public bool IsTheSelectingCard(Card card) => card == _hoveringCard;
}
