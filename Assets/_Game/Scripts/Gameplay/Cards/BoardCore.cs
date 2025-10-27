using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardCore : MonoBehaviour
{
    [SerializeField] private Vector2 cardSize = new Vector2(1f, 1f);
    [SerializeField] private Card cardPrefab;
    [SerializeField] private CardInforSO _startCardSO;
    [SerializeField] private CardInforSO _goalTreasureCardSO;
    [SerializeField] private CardInforSO _goalEmtyCardSO;
    private readonly int _rows = 5;
    private readonly int _cols = 9;
    [SerializeField] private Vector2Int _startPos = new Vector2Int(2, 0);
    [SerializeField] private List<Vector2Int> _goalPos; // x row, y col)
    private Card[,] _board;
    public readonly List<Vector2Int> ValidSlots = new();

    private void Start()
    {
        GenerateBoard();
    }

    void GenerateBoard()
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
                if (temp == _startPos)
                {
                    card.SetData(_startCardSO, CardLocation.OnBoard);
                }
                else if (_goalPos.Contains(temp))
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
                }
                _board[r, c] = card;
            }
        }
    }

    public void PlaceCardAt(Card card, Vector2Int slot)
    {
        if (!IsInsideBoard(slot)) return;
        _board[slot.x, slot.y] = card;
        card.transform.SetPositionAndRotation(GetWorldPositionForSlot(slot), Quaternion.Euler(90f, 0f, 0f));
        card.Location = CardLocation.OnBoard;
    }

    public void HoverCardAt(Card card, Vector2Int slot)
    {
        if (!IsInsideBoard(slot)) return;
        card.transform.SetPositionAndRotation(GetWorldPositionForSlot(slot), Quaternion.Euler(90f, 0f, 0f));
    }

    public List<Vector2Int> GetValidSlotsForCard(Card card)
    {
        ValidSlots.Clear();

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                if (_board[r, c] == null || _board[r, c].CardType != CardType.None)
                    continue;

                if (CanPlaceCardAt(card, new Vector2Int(r, c)))
                    ValidSlots.Add(new Vector2Int(r, c));
            }
        }
        return ValidSlots;
    }

    private bool CanPlaceCardAt(Card card, Vector2Int cardSlot)
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

            bool match = card.Connections[d] && neighborCard.Connections[opposite]
                        || card.Connections[opposite] && neighborCard.Connections[d];
            if (match) connected = true;
            else if (card.Connections[d] != neighborCard.Connections[opposite])
                return false;
        }
        return connected;
    }

    private Vector2Int GetNeighbor(Vector2Int pos, Direction dir)
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

    bool IsInsideBoard(Vector2Int pos) => pos.x >= 0 && pos.x < _rows && pos.y >= 0 && pos.y < _cols;
}