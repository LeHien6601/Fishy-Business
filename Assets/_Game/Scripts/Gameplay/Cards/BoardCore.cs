using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BoardCore : MonoBehaviour
{
    [SerializeField] private Vector2 cardSize = new Vector2(2f, 3f);
    [SerializeField] private Card cardPrefab;
    [SerializeField] private CardInforSO _startCardSO;
    [SerializeField] private CardInforSO _goalTreasureCardSO;
    [SerializeField] private CardInforSO _goalEmtyCardSO;
    [SerializeField] private Material _highlightMaterial;
    [SerializeField] private Transform _fromHandToBoardPos;
    private readonly int _rows = 5;
    private readonly int _cols = 9;
    private Card[,] _board;
    public readonly Vector2Int StartPos = new(2, 0);
    public readonly List<Vector2Int> GoalPos = new() { new(0, 8), new(2, 8), new(4, 8) }; // valid slots for a map card
    public readonly List<Vector2Int> ValidSlots = new(); // valid slots for a path card
    public readonly List<Vector2Int> OnBoardPaths = new(); // valid slots for a bomb card

    public const float FromHandToBoardDuration = 0.3f;
    public const float PlaceToSlotDuration = 0.2f;

    public void GenerateBoard()
    {
        _board = new Card[_rows, _cols];
        int randomGoal = UnityEngine.Random.Range(0, 3);
        int goalIndex = 0;
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                Vector3 pos = new Vector3(c * cardSize.x, 0f, r * cardSize.y);
                Card card = Instantiate(cardPrefab, transform);
                card.transform.localPosition = pos;
                Quaternion rotate = cardPrefab.transform.localRotation;
                Quaternion goalRotate = Quaternion.Euler(rotate.eulerAngles.x + 180f, rotate.eulerAngles.y, rotate.eulerAngles.z);
                card.transform.localRotation = rotate;
                card.name = $"Card_{r}_{c}";
                Vector2Int temp = new Vector2Int(r, c);
                if (temp == StartPos)
                {
                    card.SetData(_startCardSO, CardLocation.OnBoard);
                }
                else if (GoalPos.Contains(temp))
                {
                    if (goalIndex == randomGoal)
                    {
                        card.SetData(_goalTreasureCardSO, CardLocation.Hidden);
                    }
                    else
                    {
                        card.SetData(_goalEmtyCardSO, CardLocation.Hidden);
                    }
                    goalIndex++;
                    card.transform.localRotation = goalRotate;
                }
                else
                {
                    card.Refresh();
                    card.gameObject.SetActive(false);
                    card.SetMaterial(_highlightMaterial);
                }
                _board[r, c] = card;
            }
        }
    }

    public void PlacePathCardAt(Card card, Vector2Int slot, Action onComplete = null)
    {
        if (card == null || !IsInsideBoard(slot)) return;

        Card placeHolder = _board[slot.x, slot.y];
        card.transform.DOMove(placeHolder.transform.position, PlaceToSlotDuration).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(placeHolder.gameObject);
            _board[slot.x, slot.y] = card;
            OnBoardPaths.Add(slot);
            onComplete?.Invoke();
        });
    }

    public bool IsConnectingToAGoal()
    {
        // @TODO: check if there is a path connects from start to a goal
        return false;
    }

    public void DropCardOntoBoard(Card card, Action onComplete = null)
    {
        card.transform.DOLocalRotate(new Vector3(90f, 0f, 0f), FromHandToBoardDuration).SetEase(Ease.OutCubic);
        card.transform.DOMove(_fromHandToBoardPos.position, FromHandToBoardDuration).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    public List<Vector2Int> GetValidSlotsForCard(Card card)
    {
        ValidSlots.Clear();

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                Card slot = _board[r, c];
                if (slot == null || slot.CardType != CardType.None)
                    continue;
                Vector2Int slotPos = new(r, c);
                if (IsValidSlot(card, slotPos))
                {
                    ValidSlots.Add(slotPos);
                    slot.gameObject.SetActive(true);
                }
                else
                    slot.gameObject.SetActive(false);
            }
        }
        return ValidSlots;
    }

    public void ClearValidSlots()
    {
        foreach (var slot in ValidSlots)
        {
            _board[slot.x, slot.y].gameObject.SetActive(false);
        }
        ValidSlots.Clear();
    }

    public bool IsValidSlot(Card card, Vector2Int cardSlot)
    {
        return IsPlacableWithCurrentRotation(card, cardSlot) || IsPlacableWithOppositeRotation(card, cardSlot);
    }

    public bool IsPlacableWithCurrentRotation(Card card, Vector2Int cardSlot)
    {
        // --- điều kiện hợp lệ ---
        // 1. Phải có ít nhất 1 ô kề nối được
        // 2. Các hướng khác không được conflict (đường phải khớp nhau)
        if (card == null) return false;
        bool connected = false;
        for (int d = 0; d < 4; d++)
        {
            Vector2Int neighbor = GetNeighbor(cardSlot, (Direction)d);
            if (!IsInsideBoard(neighbor)) continue;

            Card neighborCard = _board[neighbor.x, neighbor.y];
            if (neighborCard == null || neighborCard.CardType != CardType.Path || neighborCard.Location == CardLocation.Hidden)
                continue;

            int opposite = (d + 2) % 4;

            if (card.Connections[d] && neighborCard.Connections[opposite])
                connected = true;
            else if (card.Connections[d] != neighborCard.Connections[opposite])
                return false;
        }
        return connected;
    }

    public bool IsPlacableWithOppositeRotation(Card card, Vector2Int cardSlot)
    {
        // --- điều kiện hợp lệ ---
        // 1. Phải có ít nhất 1 ô kề nối được
        // 2. Các hướng khác không được conflict (đường phải khớp nhau)
        if (card == null) return false;
        bool connected = false;
        for (int d = 0; d < 4; d++)
        {
            Vector2Int neighbor = GetNeighbor(cardSlot, (Direction)d);
            if (!IsInsideBoard(neighbor)) continue;

            Card neighborCard = _board[neighbor.x, neighbor.y];
            if (neighborCard == null || neighborCard.CardType != CardType.Path || neighborCard.Location == CardLocation.Hidden)
                continue;

            int opposite = (d + 2) % 4;

            if (card.Connections[opposite] && neighborCard.Connections[opposite])
                connected = true;
            else if (card.Connections[opposite] != neighborCard.Connections[opposite])
                return false;
        }
        return connected;
    }

    public Card GetOnBoardCard(Vector2Int slot)
    {
        if (IsInsideBoard(slot))
        {
            return _board[slot.x, slot.y];
        }
        return null;
    }

    public void BombThisPath(Vector2Int slot)
    {
        if (!OnBoardPaths.Contains(slot))
        {
            Debug.LogWarning("BombThisPath: expect a path card");
            return;
        }
        OnBoardPaths.Remove(slot);
        Card card = _board[slot.x, slot.y];
        card.Refresh();
        card.SetMaterial(_highlightMaterial);
        card.gameObject.SetActive(false);
    }

    public void ShowThisGoalCard(Vector2Int slot, Transform showTarget, bool isTressure, bool revealCardData)
    {
        if (!GoalPos.Contains(slot))
        {
            Debug.LogWarning("ShowThisGoalCard: expect a goal card");
            return;
        }

        Card goalCard = _board[slot.x, slot.y];
        if (revealCardData)
        {
            goalCard.SetData(isTressure ? _goalTreasureCardSO : _goalEmtyCardSO, CardLocation.Hidden); // still a goal card, stay hidden
        }
        Transform cardTf = goalCard.transform;
        Vector3 originalPos = cardTf.position;
        Quaternion originalRot = cardTf.rotation;

        Sequence seeGoalCardSeq = DOTween.Sequence();
        seeGoalCardSeq.Append(cardTf.DOMove(showTarget.position, 0.5f))
                    .Join(cardTf.DORotateQuaternion(showTarget.rotation, 0.5f))
                    .AppendInterval(1f)
                    .Append(cardTf.DOMove(originalPos, 0.5f))
                    .Join(cardTf.DORotateQuaternion(originalRot, 0.5f));
    }

    public Vector2Int GetNeighbor(Vector2Int pos, Direction dir)
    {
        return dir switch
        {
            Direction.N => new Vector2Int(pos.x + 1, pos.y),
            Direction.E => new Vector2Int(pos.x, pos.y + 1),
            Direction.S => new Vector2Int(pos.x - 1, pos.y),
            Direction.W => new Vector2Int(pos.x, pos.y - 1),
            _ => pos,
        };
    }

    public Vector3 GetWorldPositionForSlot(Vector2Int slot)
    {
        if (!IsInsideBoard(slot)) return Vector3.zero;
        Card c = _board[slot.x, slot.y];
        if (c == null) return Vector3.zero;
        return c.transform.position;
    }

    public bool IsInsideBoard(Vector2Int pos) => pos.x >= 0 && pos.x < _rows && pos.y >= 0 && pos.y < _cols;
}