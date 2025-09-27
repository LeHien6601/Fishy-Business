using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int rows = 5;
    public int cols = 9;
    public Vector2 cardSize = new Vector2(3, 2);  // X = width, Y = height
    public GameObject cardPrefab;

    private Card[,] board;

    [Header("Gameplay Settings")]
    public Vector2Int startPos = new Vector2Int(0, 0);
    public Vector2Int goalPos = new Vector2Int(4, 8);

    void Start()
    {
        GenerateBoard();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            bool canReach = CheckPath();
            Debug.Log("Check Path: " + (canReach ? "REACHABLE!" : "BLOCKED!"));
        }
    }

    void GenerateBoard()
    {
        board = new Card[rows, cols];
        Vector3 origin = transform.position;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = origin + new Vector3(c * cardSize.x, 0, r * cardSize.y);
                GameObject go = Instantiate(cardPrefab, pos, Quaternion.identity, transform);
                go.transform.localRotation = cardPrefab.transform.localRotation;
                go.name = $"Card_{r}_{c}";
                Card card = go.GetComponent<Card>();
                card.Refresh();
                board[r, c] = card;
            }
        }
    }

    bool CheckPath()
    {
        if (board == null) return false;

        bool[,] visited = new bool[rows, cols];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(startPos);
        visited[startPos.x, startPos.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == goalPos) return true;

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
}
