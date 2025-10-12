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
    public static float CardThickness = 0.01f;

    [SerializeField] private Card cardPrefab;
    [SerializeField] private Quaternion rotationOffset = Quaternion.identity;

    private readonly List<Card> handCards = new();
    public int CardCount => handCards.Count;



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

        UpdateCardPositions();
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
        card.Holder = null;
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

    /// <summary>
    /// Chọn card, đẩy nó lên một chút (hiệu ứng chọn)
    /// </summary>
    public void SelectCard(int index)
    {
        if (index < 0 || index >= handCards.Count) return;

        Transform card = handCards[index].transform;
        Vector3 endPos = card.position + card.up * 0.1f;
        card.DOMove(endPos, AnimationDuration).SetEase(Ease.OutBack);
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

    public (Vector3, Quaternion) GetCardPositionAndRotationPublic(int index)
    {
        return GetCardPositionAndRotation(index);
    }

    public bool IsEmpty() => handCards.Count == 0;
}
