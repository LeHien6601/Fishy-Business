using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class BoardManager : NetworkBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int rows = 5;
    [SerializeField] private int cols = 9;
    [SerializeField] private Vector2 cardSize = new Vector2(2, 3);  // X = width, Y = height
    [SerializeField] private Card cardPrefab;
    [SerializeField] private CardInforSO _startCardInfor;
    [SerializeField] private CardInforSO _goalEmtyCardInfor;
    [SerializeField] private CardInforSO _goalTreasureCardInfor;

    [Header("Deal Cards")]
    [SerializeField] private Transform deckPosition;
    [SerializeField] private List<CardHolder> playerHand;
    [SerializeField] private List<CardInforSO> availableCards;

    [Header("Deck Config")]
    [SerializeField] private int copiesPerCard = 4; // tổng 40 lá nếu availableCards = 10
    [SerializeField] private float cardStackOffset = 0.002f;

    private List<Card> deckObjects = new List<Card>(); // chỉ cần giữ object vật lý

    [Header("Gameplay Settings")]
    [SerializeField] private Vector2Int startPos = new Vector2Int(0, 0);
    [SerializeField] private List<Vector2Int> goalPos;


    // Private paramater
    private Card[,] board;
    // private int _currentPlayer = 0;
    // placing state
    private Card _placingCard = null;
    public CardHolder _originalHolder;
    private bool[] _placingOriginalConnections = null;
    [SerializeField] private Quaternion _placingOriginalLocalRot;
    private List<Vector2Int> _validSlots = new();
    private Vector2Int? _hoverSlot = null;
    private float _snapDistance = 1.5f; // threshold để snap tới ô gần nhất
    private bool _placing = false;
    private float _placingOffset = 0.05f;
    public int _playerTurnId;
    private bool _manualRotated = false; // check rotate of placing card
                                         // Bomb selecting state
    private bool _bombSelecting = false;
    private Card _bombActionCard = null;
    private CardHolder _bombOriginalHolder = null;
    private List<Vector2Int> _bombTargets = new();

    [SerializeField] private bool _isDev;
    void Start()
    {
        if (_isDev == true)
            StartGame();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            bool canReach = CheckPath();
            Debug.Log("Check Path: " + (canReach ? "REACHABLE!" : "BLOCKED!"));
        }
        // nếu đang đặt bài, xử lý input + di chuột
        if (_placing)
        {
            HandlePlacingInput();
        }
        else if (_bombSelecting)
        {
            HandleBombSelectionInput();
        }
    }

    #region Multiplayer
    private List<int> sharedDeckIds = new();
    public void InitializeFromDeck(int[] sharedDeckArray)
    {
        sharedDeckIds = new List<int>(sharedDeckArray);
    }

    // public override void OnNetworkSpawn()
    // {
    //     GameplayManager.Instance.RegisterBoard(OwnerClientId, this);

    //     if (IsOwner)
    //     {
    //         // Xoay hướng board theo player
    //         transform.rotation = Quaternion.Euler(0, 180f * (int)(OwnerClientId % 2), 0);
    //     }
    // }
    // Khi nhận từ GameplayManager RPC
    public void ApplyPlayCard(int cardId, Vector2Int slot, bool flipped)
    {
        var info = availableCards[cardId % availableCards.Count];
        Card slotCard = board[slot.x, slot.y];
        slotCard.PlaceCard(info, CardLocation.OnBoard, flipped);
    }

    #endregion

    #region Start game

    [ContextMenu("Start Game")]

    public void StartGame()
    {
        GenerateBoard();
        InitializeDeck();   // Create card deck
        // BuildDeckFromShared();
        StartCoroutine(DealCardsCoroutine());   // Deal Cards
    }

    private void BuildDeckFromShared()
    {
        deckObjects.Clear();
        foreach (int cardId in sharedDeckIds)
        {
            var info = availableCards[cardId % availableCards.Count];
            var card = Instantiate(cardPrefab, deckPosition);
            card.SetData(info, CardLocation.Deck);
            card.OnClickCard += OnClickCardRpc;
            deckObjects.Add(card);
        }
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

            card.SetData(tempList[i], CardLocation.Deck);
            card.SetLocation(CardLocation.Deck);
            card.OnClickCard += OnClickCardRpc;


            deckObjects.Add(card);
        }

        Debug.Log($"✅ Created physical deck with {deckObjects.Count} cards.");
    }

    private IEnumerator DealCardsCoroutine()
    {
        float dealDelay = 0.05f;
        int cardsPerPlayer = 10;

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
                cardObj.SetLocation(CardLocation.None);

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
                cardObj.SetHolder(player);

                yield return seq.WaitForCompletion();
                yield return new WaitForSeconds(dealDelay);
            }
        }
    }

    [ContextMenu("Gen Board")]
    void GenerateBoard()
    {
        board = new Card[rows, cols];
        int randomGoal = Random.Range(0, 3);
        int goalIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = new Vector3(c * cardSize.x, 0f, r * cardSize.y);
                Card card = Instantiate(cardPrefab, transform);
                card.transform.localPosition = pos;
                Quaternion rotate = cardPrefab.transform.localRotation;
                Quaternion goalRotate = rotate;
                if (goalRotate.x == 0)
                    goalRotate.x = 180f;
                else if (goalRotate.x == 90)
                    goalRotate.x = -90f;
                else if (goalRotate.x == -90)
                    goalRotate.x = 90f;
                card.transform.localRotation = rotate;
                card.name = $"Card_{r}_{c}";
                Vector2Int temp = new Vector2Int(r, c);
                if (temp == startPos)
                {
                    card.SetData(_startCardInfor, CardLocation.OnBoard);
                }
                else if (goalPos.Contains(temp))
                {
                    card.transform.localRotation = goalRotate;
                    if (goalIndex == randomGoal)
                    {
                        card.SetData(_goalTreasureCardInfor, CardLocation.Hidden);
                    }
                    else
                    {
                        card.SetData(_goalEmtyCardInfor, CardLocation.Hidden);
                    }
                    goalIndex++;
                }
                else
                {
                    card.Refresh();
                }
                board[r, c] = card;
            }
        }
    }
    #endregion

    #region Get CardHolder
    public void GetCardHolder(CardHolder cardHolder)
    {
        if (playerHand == null)
        {
            playerHand = new();
        }
        playerHand.Add(cardHolder);
    }
    #endregion

    #region Handle PlayGame

    // [Rpc(SendTo.ClientsAndHost)]
    private void OnClickCardRpc(Card card, CardHolder cardHolder)
    {
        if (card.Location == CardLocation.PlayerHand)
        {
            if (card.CardType == CardType.Path)
            {
                Debug.Log($"🃏 Card clicked: {card.CardInforSO.name}");
                if (cardHolder != null)
                {
                    Card removed = cardHolder.UseCard(card);
                    if (removed != null)
                    {
                        StartPlacing(removed, cardHolder);
                    }
                }
            }
            else if (card.CardType == CardType.Action)
            {
                if (card.ActionCardType == ActionCardType.BrokenTool)
                {

                }
                else if (card.ActionCardType == ActionCardType.FixTool)
                {

                }
                else if (card.ActionCardType == ActionCardType.Bomb)
                {
                    if (cardHolder != null)
                    {
                        Card removed = cardHolder.UseCard(card); // remove from hand
                        if (removed != null)
                        {
                            StartBombSelection(removed, cardHolder);
                        }
                    }
                }
                else if (card.ActionCardType == ActionCardType.CheckGold)
                {

                }
            }
        }
    }
    #region Handle Bomb
    private void StartBombSelection(Card bombCard, CardHolder originalHolder)
    {
        if (bombCard == null) return;

        // Cancel any placing state first
        if (_placingCard != null)
        {
            CancelPlacing(true);
        }

        // Clear any previous bomb state
        ClearBombSelection();

        _bombActionCard = bombCard;
        _bombOriginalHolder = originalHolder;
        _bombSelecting = true;

        // Build list of targets: Path cards that are OnBoard, exclude start and goal slots
        _bombTargets.Clear();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2Int pos = new Vector2Int(r, c);
                // skip start and goal positions
                if (pos == startPos) continue;
                if (goalPos.Contains(pos)) continue;

                Card boardCard = board[r, c];
                if (boardCard == null) continue;
                if (boardCard.CardType != CardType.Path) continue;
                if (boardCard.Location != CardLocation.OnBoard) continue;

                _bombTargets.Add(pos);
            }
        }

        if (_bombTargets.Count == 0)
        {
            Debug.Log("⚠️ Không có path card nào để phá bằng Bomb.");
            // trả lại bomb về tay
            CancelBombSelection(true);
            return;
        }

        // highlight targets
        HighlightBombTargets(true);

        // (tương tự StartPlacing) bật chế độ input
        // chúng ta sẽ xử lý input trong Update -> HandleBombSelectionInput
    }
    private void HandleBombSelectionInput()
    {
        if (!IsOwner) return; // chỉ owner thao tác

        // ESC -> cancel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelBombSelection(true); // trả bomb về tay
            return;
        }

        // Right click -> cancel selection and return bomb
        if (Input.GetMouseButtonDown(1))
        {
            CancelBombSelection(true);
            return;
        }

        // Left click -> chọn target (raycast)
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Card clickedCard = hit.collider.GetComponentInParent<Card>();
                if (clickedCard != null)
                {
                    // tìm vị trí của card trên board
                    Vector2Int? found = null;
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            if (board[r, c] == clickedCard)
                            {
                                found = new Vector2Int(r, c);
                                break;
                            }
                        }
                        if (found.HasValue) break;
                    }

                    if (found.HasValue && _bombTargets.Contains(found.Value))
                    {
                        // Hợp lệ -> phá card
                        DestroyPathCardAt(found.Value);
                        // Bomb card đã dùng -> bỏ đi (destroy object)
                        if (_bombActionCard != null)
                            Destroy(_bombActionCard.gameObject);

                        // clear selection
                        ClearBombSelection();
                        return;
                    }
                    else
                    {
                        Debug.Log("⚠️ Ô được click không hợp lệ để phá (không nằm trong target).");
                    }
                }
            }
        }
    }
    private void HighlightBombTargets(bool highlight)
    {
        foreach (var pos in _bombTargets)
        {
            if (IsInside(pos) && board[pos.x, pos.y] != null)
            {
                board[pos.x, pos.y].SetHighlight(highlight);
            }
        }
    }

    private void ClearBombSelection()
    {
        if (_bombTargets != null && _bombTargets.Count > 0)
        {
            HighlightBombTargets(false);
        }
        _bombTargets.Clear();
        _bombSelecting = false;
        _bombActionCard = null;
        _bombOriginalHolder = null;
    }

    private void CancelBombSelection(bool returnToHand)
    {
        if (_bombActionCard == null)
        {
            ClearBombSelection();
            return;
        }

        // remove highlights
        HighlightBombTargets(false);

        if (returnToHand && _bombOriginalHolder != null)
        {
            // trả lá bomb về tay
            Card card = _bombActionCard;
            Transform origHolderTransform = _bombOriginalHolder.transform;
            card.transform.SetParent(origHolderTransform, false);
            card.transform.SetAsLastSibling();
            card.ResetRotate();
            card.transform.DORotateQuaternion(card.transform.localRotation, 0.1f);

            Vector3 targetPos = _bombOriginalHolder.transform.position;
            card.transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    card.SetLocation(CardLocation.PlayerHand);
                    _bombOriginalHolder.AddCard(card);
                });
        }
        else
        {
            // bỏ lá down
            if (_bombActionCard != null)
                Destroy(_bombActionCard.gameObject);
        }

        ClearBombSelection();
    }

    /// <summary>
    /// Thực hiện hành động hủy 1 path card tại vị trí (gọi Refresh trên ô đó).
    /// Bạn có thể thêm animation ở đây nếu muốn.
    /// </summary>
    private void DestroyPathCardAt(Vector2Int slot)
    {
        if (!IsInside(slot)) return;
        Card c = board[slot.x, slot.y];
        if (c == null) return;
        Vector3 originScale = c.transform.localScale;
        Vector3 originPos = c.transform.position;
        // animation nhỏ: nâng lên rồi biến mất (tuỳ bạn có DOTween)
        Sequence seq = DOTween.Sequence();
        seq.Append(c.transform.DOMove(c.transform.position + Vector3.up * 0.4f, 0.08f));
        seq.Append(c.transform.DOScale(0f, 0.12f));
        c.transform.DOMove(originPos, 0.08f);
        seq.OnComplete(() =>
        {
            // Reset physical object -> giữ ô vật lý (board[slot] vẫn tồn tại) nhưng làm refresh
            c.Refresh();
            c.transform.localScale = originScale;
            c.transform.position = GetWorldPositionForSlot(slot);
        });
    }

    #endregion

    /// <summary>
    /// Kiểm tra tất cả các vị trí có thể đặt path card
    /// </summary>
    public List<Vector2Int> GetValidSlotsForCard(Card card)
    {
        List<Vector2Int> valid = new();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (board[r, c] == null || board[r, c].CardType != CardType.None)
                    continue;

                if (CanPlaceCardAt(card, new Vector2Int(r, c)))
                    valid.Add(new Vector2Int(r, c));
            }
        }
        return valid;
    }

    public bool CanPlaceCardAt(Card card, Vector2Int pos)
    {
        // --- điều kiện hợp lệ ---
        // 1. Phải có ít nhất 1 ô kề nối được
        // 2. Các hướng khác không được conflict (đường phải khớp nhau)
        if (card == null) return false;
        bool connected = false;
        for (int d = 0; d < 4; d++)
        {
            Vector2Int neighbor = GetNeighbor(pos, (Direction)d);
            if (!IsInside(neighbor)) continue;

            Card neighborCard = board[neighbor.x, neighbor.y];
            if (neighborCard == null || neighborCard.CardType != CardType.Path || neighborCard.Location == CardLocation.Hidden)
                continue;

            int opposite = (d + 2) % 4;

            bool match = card.Connections[d] && neighborCard.Connections[opposite];
            if (match) connected = true;
            else if (card.Connections[d] != neighborCard.Connections[opposite])
                return false;
        }

        return connected;
    }

    // Gọi từ Card.OnPointerClick sau khi RemoveCard từ Holder
    public void StartPlacing(Card card, CardHolder originalHolder)
    {
        if (card == null) return;
        if (_placingCard != null)
        {
            // đang có bài đang đặt -> hủy trước
            CancelPlacing(true);
        }
        _placingCard = card;
        _originalHolder = originalHolder;
        _manualRotated = false;
        _placingOriginalLocalRot = card.transform.localRotation;
        if (card.Connections != null)
        {
            _placingOriginalConnections = (bool[])card.Connections.Clone();
        }
        else _placingOriginalConnections = null;

        // set layer/parent để tiện di chuyển
        _placingCard.transform.SetParent(transform.parent, true); // đặt lên 1 level cao hơn board để khỏi ảnh hưởng
        _placingCard.SetLocation(CardLocation.OnBoard);

        // tính các ô hợp lệ (ban đầu dùng current orientation)
        _validSlots = GetValidSlotsForCard(_placingCard);
        // nếu không có ô hợp lệ với orientation hiện tại -> thử auto-rotate để tìm orientation phù hợp
        if (_validSlots.Count == 0 && _placingOriginalConnections != null)
        {
            for (int flip = 0; flip <= 1; flip++)
            {
                bool[] rotated = RotateConnectionsSimulate(_placingOriginalConnections, flip == 1);
                // bool[] rotated = _placingCard.Connections;
                List<Vector2Int> temp = new();
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        if (board[r, c] != null && board[r, c].CardType == CardType.None)
                            if (CanPlaceCardAtWithConnections(new Vector2Int(r, c), rotated))
                                temp.Add(new Vector2Int(r, c));
                if (temp.Count > 0)
                {
                    // apply rotation to the visual and actual card (auto-rotate as user requested)
                    for (int j = 0; j < flip; j++) _placingCard.Rotate();
                    _validSlots = temp;
                    break;
                }
            }
        }

        // ✅ Nếu vẫn không có chỗ nào đặt được -> log và return
        if (_validSlots.Count == 0)
        {
            Debug.LogWarning($"⚠️ Card '{_placingCard.CardInforSO.name}' cannot be placed on board at any position!");
            CancelPlacing(true);
            return;
        }

        HighlightSlots(_validSlots, true);

        StartCoroutine(WaitForPlacing());

    }
    private IEnumerator WaitForPlacing()
    {
        yield return new WaitForSeconds(0.2f);
        _placing = true;
    }

    // Cancel placing: nếu cancelled = true -> trả bài về tay (origin holder)
    public void CancelPlacing(bool returnToHand)
    {
        if (_placingCard == null) return;
        _placing = false;
        _manualRotated = false;
        DOTween.Kill(_placingCard.transform);
        ClearSlotHighlights();

        if (returnToHand && _originalHolder != null)
        {
            // capture local reference
            Card card = _placingCard;
            Transform origHolderTransform = _originalHolder.transform;

            card.transform.SetParent(origHolderTransform, false);
            card.transform.SetAsLastSibling();
            card.ResetRotate();
            card.transform.DORotateQuaternion(_placingOriginalLocalRot, 0.1f);

            Vector3 targetPos = _originalHolder.transform.position;
            card.transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    Debug.Log("Complete Return 1");
                    card.SetLocation(CardLocation.PlayerHand);
                    Debug.Log("Complete Return 2");
                    _originalHolder.AddCard(card);
                    Debug.Log("Complete Return 3");

                    // now it's safe to clear fields
                    if (_placingCard == card) _placingCard = null;
                    _originalHolder = null;
                });
        }
        else
        {
            if (_originalHolder == null)
            {
                Debug.Log("Destroy card: Holder null");
            }
            Destroy(_placingCard.gameObject);
            _placingCard = null;
            _originalHolder = null;
        }

        // IMPORTANT: don't null _placingCard here if you rely on the callback using it.
        // _placingCard = null;  <-- remove this line (we moved nulling into OnComplete)
        _validSlots.Clear();
        _hoverSlot = null;
    }


    private void PlaceCardToSlot(Vector2Int slot)
    {
        if (_placingCard == null) return;
        if (!IsInside(slot)) return;
        if (!_validSlots.Contains(slot)) return;

        // đặt dữ liệu lên ô board thực tế
        Card slotCard = board[slot.x, slot.y];
        if (slotCard == null) return;




        // Optional: hiệu ứng chuyển động
        Sequence seq = DOTween.Sequence();
        seq.Append(_placingCard.transform.DOMove(slotCard.transform.position + Vector3.up * 0.2f, 0.08f));
        seq.Append(_placingCard.transform.DOMove(slotCard.transform.position, 0.08f));
        seq.OnComplete(() =>
        {
            // gán data từ _placingCard vào ô slotCard
            slotCard.PlaceCard(_placingCard.CardInforSO, CardLocation.OnBoard, _manualRotated);
            slotCard.SetLocation(CardLocation.OnBoard);

            // destroy physical placing card (để tránh duplicate)
            Destroy(_placingCard.gameObject);
            // xoá highlight
            ClearSlotHighlights();
            _placingCard = null;
            _originalHolder = null;
            _validSlots.Clear();
            _hoverSlot = null;
            _manualRotated = false;
        });
    }

    private void HandlePlacingInput()
    {
        if (!IsOwner) return;
        // Right click -> rotate visually & connections
        if (Input.GetMouseButtonDown(1))
        {
            OnClickRightMouseRpc();
            return;
        }

        // ESC -> cancel & return to hand
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // restore original orientation if we mutated it and then return
            OnClickCancelCardRpc();
            return;
        }
        // Move placing card to nearest valid slot under mouse
        MovePlacingCardRpc();

        // Left click -> place if hover valid
        if (Input.GetMouseButtonDown(0))
        {
            OnClickLeftMouseRpc();
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void MovePlacingCardRpc()
    {
        Vector3 mouseWorld = GetMouseWorldPointOnBoard();
        if (mouseWorld != Vector3.zero)
        {
            // find nearest valid slot
            float bestDist = float.MaxValue;
            Vector2Int? best = null;
            foreach (var s in _validSlots)
            {
                Vector3 wp = GetWorldPositionForSlot(s);
                float d = Vector3.Distance(mouseWorld, wp);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }

            if (best.HasValue && bestDist <= _snapDistance)
            {
                // hover this slot
                if (!_hoverSlot.HasValue || _hoverSlot.Value != best.Value)
                {
                    // update rotation to best fit (auto)
                    Vector3 angle = GetBestVector3RotationForCard(_placingCard, best.Value);
                    _placingCard.transform.DOLocalRotate(angle, 0.06f).SetEase(Ease.OutQuad);
                    // _manualRotated = IsCardFlipped();
                    // _placingCard.SetConnectionByRotate(_manualRotated);
                    _hoverSlot = best.Value;
                }

                // move card visually to this slot position (slightly above)
                Vector3 targetPos = GetWorldPositionForSlot(best.Value) + Vector3.up * _placingOffset;
                _placingCard.transform.DOMove(targetPos, 0.04f).SetEase(Ease.OutQuad);
            }
        }
    }
    // [Rpc(SendTo.ClientsAndHost)]
    private void OnClickCancelCardRpc()
    {
        Debug.Log("Return card!");
        _placing = false;
        CancelPlacing(true);
    }
    // [Rpc(SendTo.ClientsAndHost)]
    private void OnClickLeftMouseRpc()
    {
        if (_hoverSlot.HasValue && _validSlots.Contains(_hoverSlot.Value))
        {
            PlaceCardToSlot(_hoverSlot.Value);
            _placing = false;
            return;
        }
        else
        {
            // click ngoài vùng hợp lệ -> không làm gì cả
            Debug.Log("⚠️ Click ngoài vùng đặt hợp lệ, bỏ qua.");
        }

    }
    // [Rpc(SendTo.ClientsAndHost)]
    private void OnClickRightMouseRpc()
    {
        // Lưu orientation cũ
        bool prevRotated = _manualRotated;

        // Xoay sang orientation mới
        _manualRotated = !_manualRotated;
        _placingCard.Rotate(_manualRotated);

        // Recalc valid slots với orientation mới
        List<Vector2Int> newValid = GetValidSlotsForCard(_placingCard);

        // Nếu không đặt được ở đâu cả => revert lại orientation cũ
        if (newValid.Count == 0)
        {
            Debug.Log("⚠️ Không thể đặt được ở bất kỳ đâu sau khi xoay — revert rotation!");
            _manualRotated = prevRotated;
            _placingCard.Rotate(_manualRotated); // xoay lại
        }
        else
        {
            // Nếu ok thì cập nhật highlight
            ClearSlotHighlights();
            _validSlots = newValid;
            HighlightSlots(_validSlots, true);
        }

    }



    // compute world position for slot center
    public Vector3 GetWorldPositionForSlot(Vector2Int slot)
    {
        if (!IsInside(slot)) return Vector3.zero;
        Card c = board[slot.x, slot.y];
        if (c == null) return Vector3.zero;
        return c.transform.position;
    }

    // trả Quaternion tốt nhất (90deg step) cho card tại slot -> thử 0..3 và chọn rotation đầu gặp CanPlaceCardAtWithConnections
    public Vector3 GetBestVector3RotationForCard(Card card, Vector2Int slot)
    {
        if (card == null || card.Connections == null) return card.transform.rotation.eulerAngles;
        // lưu trạng thái ban đầu
        bool[] orig = (bool[])card.CardInforSO.Connections.Clone();


        // 0° (normal)
        if (CanPlaceCardAtWithConnections(slot, RotateConnectionsSimulate(orig, false)))
        {
            _manualRotated = false;
            _placingCard.SetConnectionByRotate(false);
            return new Vector3(90f, 0f, 0f);
        }

        // 180° (flipped)
        if (CanPlaceCardAtWithConnections(slot, RotateConnectionsSimulate(orig, true)))
        {
            _manualRotated = true;
            _placingCard.SetConnectionByRotate(true);
            return new Vector3(90f, 180f, 0f);
        }
        // nếu không có rotation nào match -> trả rotation hiện tại
        return card.transform.rotation.eulerAngles;
    }

    // helper: rotate connections simulate by step 1 => lên xuống quay vòng (N,E,S,W)
    private bool[] RotateConnectionsSimulate(bool[] con, bool flipped)
    {
        if (con == null || con.Length < 4) return con;
        // bool[] newCon = (bool[])con.Clone();
        if (flipped == false)
            return con;

        bool[] flippedCon = new bool[4];
        flippedCon[0] = con[2];
        flippedCon[1] = con[3];
        flippedCon[2] = con[0];
        flippedCon[3] = con[1];


        return flippedCon;
    }

    private void HighlightSlots(List<Vector2Int> slots, bool highlight)
    {
        foreach (var s in slots)
        {
            if (IsInside(s) && board[s.x, s.y] != null)
            {
                board[s.x, s.y].SetHighlight(highlight);
            }
        }
    }

    private void ClearSlotHighlights()
    {
        // clear all highlights on board (inefficient but đơn giản)
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (board[r, c] != null)
                    board[r, c].SetHighlight(false);
    }

    #endregion

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

    #region Helper
    // overload helper: kiểm tra với một mảng connections bất kỳ (dùng cho simulate rotate)
    private bool CanPlaceCardAtWithConnections(Vector2Int pos, bool[] connections)
    {
        if (connections == null || connections.Length < 4) return false;
        bool connected = false;
        for (int d = 0; d < 4; d++)
        {
            Vector2Int neighbor = GetNeighbor(pos, (Direction)d);
            if (!IsInside(neighbor)) continue;

            Card neighborCard = board[neighbor.x, neighbor.y];
            if (neighborCard == null || neighborCard.CardType != CardType.Path || neighborCard.Location != CardLocation.OnBoard)
                continue;

            int opposite = (d + 2) % 4;

            bool match = connections[d] && neighborCard.Connections[opposite];
            if (match) connected = true;
            else
                return false;
        }

        return connected;
    }

    // tính point trên mặt board từ vị trí chuột (raycast plane)
    private Vector3 GetMouseWorldPointOnBoard()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane p = new Plane(Vector3.up, transform.position); // plane ngang đi qua vị trí board
        if (p.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            return hit;
        }
        return Vector3.zero;
    }

    // trả về true nếu card hiện đang là flipped so với original connections
    private bool IsCardFlipped()
    {
        if (_placingOriginalConnections == null || _placingOriginalConnections.Length < 4) return false;
        if (_placingCard.CardInforSO == null) return false;
        bool[] flipped = (bool[])_placingCard.CardInforSO.Connections.Clone();
        bool[] cur = _placingCard.Connections;
        for (int i = 0; i < 4; i++)
            if (cur[i] != flipped[i]) return false;
        return true;
    }
    #endregion
}
