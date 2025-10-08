using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int rows = 5;
    public int cols = 9;
    public Vector2 cardSize = new Vector2(2, 3);  // X = width, Y = height
    public Card cardPrefab;
    [SerializeField] private CardInforSO _startCardInfor;
    private Card[,] board;

    [Header("Deal Cards")]
    [SerializeField] private Transform deckPosition;
    [SerializeField] private List<CardHolder> playerHand;
    [SerializeField] private List<CardInforSO> availableCards;
    [Header("Deck Config")]
    [SerializeField] private int copiesPerCard = 4; // tổng 40 lá nếu availableCards = 10
    [SerializeField] private float cardStackOffset = 0.002f;

    private List<Card> deckObjects = new List<Card>(); // chỉ cần giữ object vật lý

    [Header("Gameplay Settings")]
    public Vector2Int startPos = new Vector2Int(0, 0);
    public List<Vector2Int> goalPos;

    void Start()
    {
        GenerateBoard();
        // DealCard();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            bool canReach = CheckPath();
            Debug.Log("Check Path: " + (canReach ? "REACHABLE!" : "BLOCKED!"));
        }
    }

    #region DealCards

    [ContextMenu("Start Game")]
    public void StartGame()
    {
        InitializeDeck();   // Create card deck
        StartCoroutine(DealCardsCoroutine());   // Deal Cards
    }

    public void InitializeDeck()
    {
        // Delete old deck 
        foreach (var c in deckObjects)
            if (c != null) DestroyImmediate(c.gameObject);

        deckObjects.Clear();

        // Create cards base on availableCards
        List<CardInforSO> tempList = new List<CardInforSO>();
        for (int i = 0; i < copiesPerCard; i++)
        {
            tempList.AddRange(availableCards);
        }

        // Shuffle (Fisher–Yates)
        for (int i = 0; i < tempList.Count; i++)
        {
            int rand = Random.Range(i, tempList.Count);
            (tempList[i], tempList[rand]) = (tempList[rand], tempList[i]);
        }

        // Instantiate a deck Cards on table
        for (int i = 0; i < tempList.Count; i++)
        {
            Card card = Instantiate(cardPrefab, deckPosition);
            card.transform.localPosition = new Vector3(0, i * cardStackOffset, 0);
            Quaternion quaternion = cardPrefab.transform.localRotation;
            quaternion.x = -quaternion.x;
            card.transform.localRotation = quaternion;

            card.SetData(tempList[i]);
            // card.SetMaterial();

            deckObjects.Add(card);
        }

        Debug.Log($"✅ Created physical deck with {deckObjects.Count} cards.");
    }

    private IEnumerator DealCardsCoroutine()
    {
        float dealDelay = 0.05f;
        int cardsPerPlayer = 5;

        for (int i = 0; i < cardsPerPlayer; i++)
        {
            foreach (var player in playerHand)
            {
                if (deckObjects.Count == 0)
                {
                    Debug.LogWarning("❌ Deck is empty!");
                    yield break;
                }

                // Draw Card on top
                int lastIndex = deckObjects.Count - 1;
                Card cardObj = deckObjects[lastIndex];
                deckObjects.RemoveAt(lastIndex);

                // Remove from deck parent to move to Player
                cardObj.transform.SetParent(null);

                // Calculate Pos and Rotate of card of player
                int playerCardIndex = player.CardCount;
                var (targetPos, targetRot) = player.GetCardPositionAndRotationPublic(playerCardIndex);

                // Effect draw card
                Vector3 liftPos = cardObj.transform.position + Vector3.up * 0.2f;

                Sequence seq = DOTween.Sequence();
                seq.Append(cardObj.transform.DOMove(liftPos, 0.03f).SetEase(Ease.OutQuad));
                seq.Append(cardObj.transform.DOMove(targetPos, 0.1f).SetEase(Ease.OutCubic));

                seq.Join(cardObj.transform.DORotate(new Vector3(0, 180, 0), 0.05f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.InOutQuad));

                seq.Append(cardObj.transform.DORotateQuaternion(targetRot, 0.05f).SetEase(Ease.OutCubic));

                player.AddCard(cardObj);

                yield return seq.WaitForCompletion();
                yield return new WaitForSeconds(dealDelay);
            }
        }
    }
    #endregion

    [ContextMenu("Gen Board")]
    void GenerateBoard()
    {
        board = new Card[rows, cols];
        Vector3 origin = transform.position;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = new Vector3(c * cardSize.x, 0f, r * cardSize.y);
                Card card = Instantiate(cardPrefab, transform);
                card.transform.localPosition = pos;
                card.transform.localRotation = cardPrefab.transform.localRotation;
                card.name = $"Card_{r}_{c}";
                Vector2Int temp = new Vector2Int(r, c);
                if (temp == startPos || goalPos.Contains(temp))
                {
                    card.SetData(_startCardInfor);
                }
                else
                {
                    card.Refresh();
                }
                board[r, c] = card;
            }
        }
    }


    #region Check Win/Lose Condition
    bool CheckPath()
    {
        if (board == null)
        {
            Debug.LogWarning("Board is null!");
            return false;
        }
        bool[,] visited = new bool[rows, cols];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(startPos);
        visited[startPos.x, startPos.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (goalPos.Contains(current)) return true;

            Card currentCard = board[current.x, current.y];
            if (currentCard == null) continue;

            // chỉ PathCard mới có thể đi
            if (currentCard.CardType != CardType.Path) continue;

            // deadend thì không đi tiếp
            if (currentCard.PathCardType == PathCardType.DeadEnd)
                continue;

            bool[] curCon = currentCard.Connections;
            if (curCon == null || curCon.Length < 4) continue;

            for (int d = 0; d < 4; d++) // N,E,S,W
            {
                if (!curCon[d]) continue;

                Vector2Int next = GetNeighbor(current, (Direction)d);
                if (!IsInside(next)) continue;

                Card nextCard = board[next.x, next.y];
                if (nextCard == null) continue;
                if (nextCard.CardType != CardType.Path) continue;

                bool[] nextCon = nextCard.Connections;
                if (nextCon == null || nextCon.Length < 4) continue;

                int opposite = (d + 2) % 4;
                if (!nextCon[opposite]) continue;

                if (!visited[next.x, next.y])
                {
                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    Vector2Int GetNeighbor(Vector2Int pos, Direction dir)
    {
        switch (dir)
        {
            case Direction.N: return new Vector2Int(pos.x + 1, pos.y);
            case Direction.E: return new Vector2Int(pos.x, pos.y + 1);
            case Direction.S: return new Vector2Int(pos.x - 1, pos.y);
            case Direction.W: return new Vector2Int(pos.x, pos.y - 1);
        }
        return pos;
    }

    bool IsInside(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < cols;
    }
    #endregion
}
