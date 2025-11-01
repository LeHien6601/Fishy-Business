using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class NetworkBoardManager : NetworkBehaviour
{
    [SerializeField] private CardDatabaseSO _cardDatabase;
    [SerializeField] private Card _cardPrefab;
    [SerializeField] private Transform _deckPlace;
    [SerializeField] private Transform _discardPile;
    private readonly Stack<Card> _cardsInDeck = new(); // represents the deck of cards to be dealt
    private readonly List<CardHolder> _cardHolders = new(); // local cache of all card holders on the board, 1 is yours, the others are dummies representing other players' hands
    private readonly Dictionary<ulong, CardHolder> _playerAndHolderMap = new();
    private readonly List<CardData> _masterDeck = new(); // only server has the full deck data
    private int _tressureIndex = -1; // only server knows this, clients if want to know must send a rpc
    private NetworkList<ulong> _playerOrders = new();
    private ulong _inTurnPlayer = ulong.MaxValue;
    private const float _waitBetweenPlayerTurns = 1f;
    private const float _actionCardDuration = 2;

    [SerializeField] private BoardCore _boardCore;
    private Plane _boardPlane; // for mouse raycast onto board
    private Card _placingCard = null; // card being placed by you or others
    private Vector2Int? _hoveringSlot = null; // the slot on board the _placingCard is hovering on
    private ulong? _targerPlayer = null; // the player that is targeted by a tool card (break/repair)
    private const float _placingOffset = 0.05f;
    private const float _snapDistance = 1f;
    private const float _deckStackSpace = 0.002f;
    private PlayerState _localPlayerState = PlayerState.NONE;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _boardPlane = new Plane(Vector3.up, transform.position);
    }

    #region SETUP
    public async void StartGameLogic(NetworkList<ulong> playerOrders)
    {
        if (!IsServer) return;

        // Perform server-only setup
        _playerOrders = playerOrders;
        InitializeAndShuffleDeck();

        // random goal tressure
        _tressureIndex = Random.Range(0, 3);

        await Task.Delay(1000); // wait for a moment to ensure all clients are ready
        StartGameClientRpc();
        DealCards(playerOrders);
        await Task.Delay(1000); // wait for a moment before starting first turn
        NextTurnClientRpc(_playerOrders[0]);
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        // spawn board, facing towards local player
        _boardCore.GenerateBoard();
        transform.rotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
        // spawn deck,
        _cardsInDeck.Clear();
        for (int i = 0; i < _cardDatabase.TotalCount(); i++)
        {
            _cardsInDeck.Push(Instantiate(_cardPrefab, _deckPlace.position + _deckStackSpace * i * Vector3.up, _cardPrefab.transform.rotation, _deckPlace));
        }
    }

    private void DealCards(NetworkList<ulong> playerOrders)
    {
        if (!IsServer) return;

        int currentDeckIndex = 0;

        foreach (ulong clientId in playerOrders)
        {
            List<CardData> hand = new List<CardData>();
            int CardsPerPlayer = CalculateCardsPerPlayer(playerOrders.Count);
            for (int i = 0; i < CardsPerPlayer; i++)
            {
                if (currentDeckIndex >= _masterDeck.Count)
                    break; // Safety break

                hand.Add(_masterDeck[currentDeckIndex++]);
            }
            // Send the specific hand to this player's client only
            DeliverHandClientRpc(hand.ToArray(), new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId } // Target only this one player
                }
            });
        }
    }

    [ClientRpc]
    private void DeliverHandClientRpc(CardData[] cardDatas, ClientRpcParams clientRpcParams)
    {
        StartCoroutine(ReceiveHand(cardDatas));
    }

    private IEnumerator ReceiveHand(CardData[] cards)
    {
        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} received {cards.Length} cards.");
        WaitForSeconds wait = new(0.1f);
        foreach (CardData cardData in cards)
        {
            foreach (CardHolder holder in _cardHolders)
            {
                Card newCard = _cardsInDeck.Pop();
                if (holder.IsMine)
                {
                    newCard.SetData(cardData, _cardDatabase.GetCardInforSO(cardData), CardLocation.Deck);
                    newCard.OnPlayCard += PlayCard;
                    newCard.OnHoverCard += HoverCardInHand;
                    newCard.OnExitHoverCard += (card) => { _boardCore.ClearValidSlots(); };
                    newCard.OnDiscardCard += DiscardCardFromHand;
                }
                holder.AddCard(newCard); // CardLocation will become PlayerHand inside AddCard
                Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} added card {cardData.CardID} to holder.");
                yield return wait;
            }
        }
    }

    private void InitializeAndShuffleDeck()
    {
        if (!IsServer) return;

        _masterDeck.Clear();
        for (int i = 0; i < _cardDatabase.Size(); i++)
        {
            // some cards have multiple copies, we still treat them with the same ID
            int amount = _cardDatabase.GetCopiesOfCard(i);
            for (int j = 0; j < amount; j++)
            {
                _masterDeck.Add(new CardData { CardID = i });
            }
        }

        // Shuffle the deck
        for (int i = 0; i < _masterDeck.Count; i++)
        {
            int randomIndex = Random.Range(0, _masterDeck.Count);
            (_masterDeck[randomIndex], _masterDeck[i]) = (_masterDeck[i], _masterDeck[randomIndex]);
        }
    }

    public void RegisterCardHolder(ulong clientId, CardHolder holder)
    {
        if (!_cardHolders.Contains(holder))
        {
            Debug.Log("resgistered client: "+ clientId);
            _cardHolders.Add(holder);
            _playerAndHolderMap[clientId] = holder;
        }
    }

    private int CalculateCardsPerPlayer(int totalPlayers)
    {
        // comment this line for testing purpose
        // if (totalPlayers < 3) return -1; 
        if (totalPlayers <= 5) return 6;
        else if (totalPlayers <= 7) return 5;
        else if (totalPlayers <= 10) return 4;
        else return -1;
    }

    #endregion

    #region PLAY CARD FROM HAND TO BOARD
    private void PlayCard(Card card)
    {
        // condition checking are already handled when hovering in hand
        card.Holder.IsTurn = false;
        PlayCardServerRpc(card.CardData, NetworkManager.Singleton.LocalClientId);
        _placingCard = card;
        card.Holder.RemoveCard(card);
        card.transform.SetParent(_boardCore.transform);
        PlayerState nextState = MatchStateWithCard(card);
        _boardCore.DropCardOntoBoard(card, onComplete: () => { _localPlayerState = nextState; });

        static PlayerState MatchStateWithCard(Card card)
        {
            if (card.CardType == CardType.Path) return PlayerState.PLACING_CARD;
            else if (card.CardType == CardType.Action)
            {
                return card.ActionCardType switch
                {
                    ActionCardType.CheckGold => PlayerState.CHECKING_GOAL,
                    ActionCardType.Bomb => PlayerState.SELECTING_PLACE_TO_BOMB,
                    ActionCardType.FixTool => PlayerState.USING_TOOL,
                    ActionCardType.BrokenTool => PlayerState.USING_TOOL,
                    _ => PlayerState.NONE
                };
            }
            return PlayerState.NONE;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayCardServerRpc(CardData arg0, ulong senderId)
    {
        List<ulong> targets = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        targets.Remove(senderId);
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = targets.ToArray() // Send to all clients except sender
            }
        };
        PlayCardOtherClientRpc(arg0, senderId, clientRpcParams);
    }

    /// <summary>
    /// Play card on other clients' board when a player plays a card
    /// See PlayCard() above for the actual player
    /// </summary>
    /// <param name="arg0"></param>
    /// <param name="senderId"></param>
    /// <param name="clientRpcParams"></param>
    [ClientRpc]
    private void PlayCardOtherClientRpc(CardData arg0, ulong senderId, ClientRpcParams clientRpcParams)
    {
        CardHolder cardHolder = _playerAndHolderMap[senderId];
        Card card = cardHolder.RemoveRandomCard();
        _placingCard = card;
        card.transform.SetParent(_boardCore.transform);
        card.SetData(_cardDatabase.GetCardInforSO(arg0), CardLocation.OnBoard);

        _boardCore.DropCardOntoBoard(card);

    }

    #endregion

    #region HOVER CARD ON BOARD
    private void HoverCardOnBoard(List<Vector2Int> slots)
    {
        Vector3 mouseWorld = GetMouseWorldPointOnBoard();
        if (mouseWorld == Vector3.zero)
            return;

        float sqrDistance = _snapDistance;
        Vector2Int? bestSlot = null;
        foreach (var slot in slots)
        {
            Vector3 wp = _boardCore.GetWorldPositionForSlot(slot);
            float sqrD = Vector3.SqrMagnitude(mouseWorld - wp);
            if (sqrD < sqrDistance)
            {
                sqrDistance = sqrD;
                bestSlot = slot;
            }
        }
        if (bestSlot.HasValue)
        {
            // hover this slot
            if (!_hoveringSlot.HasValue || _hoveringSlot.Value != bestSlot.Value)
            {
                _hoveringSlot = bestSlot.Value;
                MovePlacingCardServerRpc(bestSlot.Value);
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void MovePlacingCardServerRpc(Vector2Int slot)
    {
        MovePlacingCardClientRpc(slot);
    }

    [ClientRpc]
    private void MovePlacingCardClientRpc(Vector2Int slot)
    {
        if (_placingCard.CardType == CardType.Path)
        {
            _hoveringSlot = slot;
            if (!_boardCore.IsPlacableWithCurrentRotation(_placingCard, slot))
            {
                _placingCard.Rotate();
            }
        }
        else if (_placingCard.ActionCardType != ActionCardType.Bomb && _placingCard.ActionCardType != ActionCardType.CheckGold)
        {
            return;
        }

        // move card visually to this slot position (slightly above)
        Vector3 targetPos = _boardCore.GetWorldPositionForSlot(slot) + Vector3.up * _placingOffset;
        _placingCard.transform.DOMove(targetPos, 0.04f).SetEase(Ease.OutQuad);
    }
    #endregion

    #region HANDLE INPUT AFTER PLAYING A CARD TO BOARD
    private void Update()
    {
        switch (_localPlayerState)
        {
            case PlayerState.PLACING_CARD:
                HoverCardOnBoard(_boardCore.ValidSlots);
                if (!_hoveringSlot.HasValue) return; // wait for a _hoveringSlot before processing any input

                if (Input.GetMouseButtonDown(0)) // left mouse = confirm
                {
                    // after placing card, wait for drawing a new card, then end turn 
                    _localPlayerState = PlayerState.NONE;
                    SendInputActionServerRpc(InputAction.CONFIRM);
                }
                if (Input.GetMouseButtonDown(1)) // right mouse = rotate
                {
                    // check condition in advance before sending RPC to save network traffic
                    if (_boardCore.IsPlacableWithOppositeRotation(_placingCard, _hoveringSlot.Value))
                    {
                        SendInputActionServerRpc(InputAction.ROTATE);
                    }
                }
                break;
            case PlayerState.SELECTING_PLACE_TO_BOMB:
                HoverCardOnBoard(_boardCore.OnBoardPaths);
                if (!_hoveringSlot.HasValue) return; // wait for a _hoveringSlot before processing any input

                if (Input.GetMouseButtonDown(0)) // left mouse = confirm
                {
                    _localPlayerState = PlayerState.NONE;
                    BombThisPathServerRpc(_hoveringSlot.Value);
                }
                break;
            case PlayerState.USING_TOOL:
                HoverTargerPlayer();
                if (!_targerPlayer.HasValue) return;

                if (Input.GetMouseButtonDown(0))
                {
                    _localPlayerState = PlayerState.NONE;
                    ApplyToolOnPlayerServerRpc(_targerPlayer.Value);
                }
                break;
            case PlayerState.CHECKING_GOAL:
                HoverCardOnBoard(_boardCore.GoalPos);
                if (!_hoveringSlot.HasValue) return; // wait for a _hoveringSlot before processing any input

                if (Input.GetMouseButtonDown(0)) // left mouse = confirm
                {
                    _localPlayerState = PlayerState.NONE;
                    CheckThisGoalServerRpc(NetworkManager.Singleton.LocalClientId, _hoveringSlot.Value);
                }
                break;
            case PlayerState.NONE:
            default:
                break;
        }
    }

    public enum InputAction
    {
        CONFIRM, // left mouse
        ROTATE, // right mouse
        DISCARD, // esc
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendInputActionServerRpc(InputAction action)
    {
        SendInputActionClientRpc(action);

        // after confirming placement, draw a new card then end turn
        if (action == InputAction.CONFIRM)
        {
            ServerDrawNewCardThenEndTurn();
        }
    }

    [ClientRpc]
    private void SendInputActionClientRpc(InputAction action)
    {
        switch (action)
        {
            case InputAction.CONFIRM:
                if (_placingCard)
                {
                    _boardCore.PlacePathCardAt(_placingCard, _hoveringSlot.Value, () =>
                    {
                        List<Vector2Int> slots = _boardCore.IsConnectingToAHiddenGoal();
                        if (slots != null || slots.Count > 0)
                        {
                            foreach (var slot in slots)
                            {
                                RevealGoalCardServerRpc(slot);
                            }
                        }
                    });
                    _boardCore.ClearValidSlots();
                    _placingCard = null;
                    _hoveringSlot = null;
                }
                break;
            case InputAction.ROTATE:
                _placingCard.Rotate();
                break;
            case InputAction.DISCARD: // not allow for now
            default:
                break;
        }
    }

    [ServerRpc]
    private void RevealGoalCardServerRpc(Vector2Int slot)
    {
        RevealGoalCardClientRpc(slot, slot.x / 2 == _tressureIndex);

    }

    [ClientRpc]
    private void RevealGoalCardClientRpc(Vector2Int slot, bool isTreasure)
    {
        _boardCore.OpenHiddenGoalCard(slot, isTreasure);
    }
    #endregion

    #region DISCARD CARD FROM HAND T0 DISCARD PILE
    private void DiscardCardFromHand(Card card)
    {
        card.Holder.IsTurn = false;
        card.Holder.RemoveCard(card);
        card.transform.SetParent(_discardPile.transform);
        card.transform.DOMove(_discardPile.transform.position + _discardPile.transform.childCount * _deckStackSpace * Vector3.up, 1f).SetEase(Ease.OutCubic);
        card.transform.DOLocalRotate(Vector3.zero, 1f).SetEase(Ease.OutCubic);
        // zero because we already edit the _discardPile pos and rot
        DiscardCardFromHandServerRpc(senderId: NetworkManager.Singleton.LocalClientId);
        _boardCore.ClearValidSlots();

    }

    [ServerRpc(RequireOwnership = false)]
    private void DiscardCardFromHandServerRpc(ulong senderId)
    {
        ServerDrawNewCardThenEndTurn();

        List<ulong> targets = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        targets.Remove(senderId);
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = targets.ToArray() // Send to all clients except sender
            }
        };
        DiscardCardFromHandOtherClientRpc(senderId, clientRpcParams);
    }

    [ClientRpc]
    private void DiscardCardFromHandOtherClientRpc(ulong senderId, ClientRpcParams clientRpcParams)
    {
        CardHolder cardHolder = _playerAndHolderMap[senderId];
        Card card = cardHolder.RemoveRandomCard();
        card.transform.SetParent(_discardPile.transform);
        card.transform.DOMove(_discardPile.transform.position + _discardPile.transform.childCount * _deckStackSpace * Vector3.up, 1f).SetEase(Ease.OutCubic);
        card.transform.DOLocalRotate(Vector3.zero, 1f).SetEase(Ease.OutCubic);
        // zero because we already edit the _discardPile pos and rot
    }

    #endregion

    #region CHECK GOAL
    [ServerRpc(RequireOwnership = false)]
    private void CheckThisGoalServerRpc(ulong requesterId, Vector2Int checkSlot)
    {
        // goal row = 0 2 4, divide by 2 is 0 1 2, exactly the indexes we want
        if (!_boardCore.GoalPos.Contains(checkSlot))
        {
            Debug.LogWarning("this slot is not in GoalPos list");
            return;
        }
        CheckThisGoalClientRpc(requesterId, checkSlot, checkResult: checkSlot.x / 2 == _tressureIndex);
        this.WaitThenExecute(_actionCardDuration, () => ServerDrawNewCardThenEndTurn());
    }

    [ClientRpc]
    private void CheckThisGoalClientRpc(ulong requesterId, Vector2Int checkSlot, bool checkResult, ClientRpcParams clientRpcParams = default)
    {
        _placingCard.transform.DOMove(_boardCore.GetWorldPositionForSlot(checkSlot), BoardCore.PlaceToSlotDuration).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(_placingCard.gameObject);
            _placingCard = null;
        });
        CardHolder requester = _playerAndHolderMap[requesterId];
        _boardCore.ShowThisGoalCard(checkSlot,
                                    showTarget: requester.BeforeFaceSlot(),
                                    isTressure: checkResult,
                                    revealCardData: NetworkManager.Singleton.LocalClientId == requesterId);
    }
    #endregion

    #region BOMB A PATH ON BOARD

    [ServerRpc(RequireOwnership = false)]
    private void BombThisPathServerRpc(Vector2Int slot)
    {
        if (!_boardCore.OnBoardPaths.Contains(slot))
        {
            Debug.LogWarning("this slot is not in OnBoardPaths list");
            return;
        }
        BombThisPathClientRpc(slot);
        this.WaitThenExecute(_actionCardDuration, () => ServerDrawNewCardThenEndTurn());
    }

    [ClientRpc]
    private void BombThisPathClientRpc(Vector2Int slot)
    {
        _placingCard.transform.DOMove(_boardCore.GetWorldPositionForSlot(slot), BoardCore.PlaceToSlotDuration).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(_placingCard.gameObject);
            _placingCard = null;
        });
        _boardCore.BombThisPath(slot);
        // @TODO: add some visuals
    }

    #endregion

    #region HOVER MOUSE ON BOARD TO CHOOSE TARGET PLAYER
    private void HoverTargerPlayer()
    {
        Vector3 mouseWorld = GetMouseWorldPointOnBoard();
        if (mouseWorld == Vector3.zero)
            return;

        float sqrDistance = float.MaxValue;
        ulong? closetPlayer = null;
        foreach (var player in _playerAndHolderMap)
        {
            Vector3 wp = player.Value.transform.position;
            float sqrD = Vector3.SqrMagnitude(mouseWorld - wp);
            if (sqrD < sqrDistance)
            {
                sqrDistance = sqrD;
                closetPlayer = player.Key;
            }
        }
        if (closetPlayer.HasValue)
        {
            // hover this slot
            if (!_targerPlayer.HasValue || _targerPlayer.Value != closetPlayer.Value)
            {
                _targerPlayer = closetPlayer.Value;
                SwitchTargerPlayerServerRpc(_targerPlayer.Value);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchTargerPlayerServerRpc(ulong targetId)
    {
        SwitchTargerPlayerClientRpc(targetId);
    }

    [ClientRpc]
    private void SwitchTargerPlayerClientRpc(ulong targetId)
    {
        _targerPlayer = targetId;
        Transform targetTf = _playerAndHolderMap[targetId].BeforeFaceSlot();
        _placingCard.transform.DOMove(targetTf.position, 0.1f);
        _placingCard.transform.DORotateQuaternion(targetTf.rotation, 0.1f);
        Debug.Log("targeting player " + targetId);
    }
    #endregion

    #region TOOL FUNCTION ON A PLAYER

    [ServerRpc(RequireOwnership = false)]
    private void ApplyToolOnPlayerServerRpc(ulong targetPlayer)
    {
        if (_placingCard.ActionCardType == ActionCardType.BrokenTool || _placingCard.ActionCardType == ActionCardType.FixTool)
        {
            ApplyToolOnPlayerClientRpc(targetPlayer);
            this.WaitThenExecute(_actionCardDuration, () => ServerDrawNewCardThenEndTurn());
        }
    }

    [ClientRpc]
    private void ApplyToolOnPlayerClientRpc(ulong tagetPlayer)
    {
        // @TODO: visualize using coins 
        bool isRepair = _placingCard.ActionCardType == ActionCardType.FixTool;
        CardHolder holder = _playerAndHolderMap[tagetPlayer];
        switch (_placingCard.ToolType)
        {
            case ToolType.Cart:
                holder.Cart = isRepair;
                break;
            case ToolType.Hat:
                holder.Hat = isRepair;
                break;
            case ToolType.Shovel:
                holder.Shovel = isRepair;
                break;
            case ToolType.CartHat:
                holder.Cart = holder.Hat = isRepair;
                break;
            case ToolType.CartShovel:
                holder.Cart = holder.Shovel = isRepair;
                break;
            case ToolType.HatShovel:
                holder.Hat = holder.Shovel = isRepair;
                break;
        }
        Destroy(_placingCard.gameObject);
        _placingCard = null;
        _targerPlayer = null;
    }

    #endregion

    #region CONDITION CHECKING WHEN HOVERING CARD IN HAND
    private void HoverCardInHand(Card card)
    {
        if (CanPlayThisCard(card))
            card.Holder.SelectCard(card);
        else
            card.Highlight(false);
    }
    private bool CanPlayThisCard(Card card)
    {
        if (card.CardType == CardType.Path)
        {
            return CanPlayPathCard(card);
        }
        else if (card.CardType == CardType.Action)
        {
            return card.ActionCardType switch
            {
                ActionCardType.Bomb => CanPlayBombCard(),
                ActionCardType.BrokenTool => CanPlayBreakCard(card.ToolType),
                ActionCardType.FixTool => CanPlayRepairCard(card.ToolType),
                _ => true,
            };
        }
        return true;
    }
    private bool CanPlayPathCard(Card card) => card.Holder.HasAllTools() && _boardCore.GetValidSlotsForCard(card).Count > 0;
    private bool CanPlayBombCard() => _boardCore.OnBoardPaths.Count > 0;
    private bool CanPlayRepairCard(ToolType toolType)
    {
        foreach (CardHolder holder in _cardHolders)
        {
            if (!holder.HasTool(toolType))
                return true;
        }
        return false;
    }
    private bool CanPlayBreakCard(ToolType toolType)
    {
        // at least 1 player has a tool left
        foreach (CardHolder holder in _cardHolders)
        {
            if (holder.IsMine) // can not play break card on urself
                continue;
            if (holder.HasTool(toolType))
                return true;
        }
        return false;
    }
    #endregion

    #region SERVER DRAW NEW CARD THEN NEXT TURN
    private void ServerDrawNewCardThenEndTurn()
    {
        if (!IsServer)
            return;
        int id = _masterDeck.Count - _cardsInDeck.Count;
        if (id > 0) // check before sending RPC to save bandwidth
        {
            DrawNewCardClientRpc(_masterDeck[id], receiver: _inTurnPlayer);
        }
        // wait for a short moment then end turn
        _inTurnPlayer = _playerOrders[(_playerOrders.IndexOf(_inTurnPlayer) + 1) % _playerOrders.Count];
        this.WaitThenExecute(_waitBetweenPlayerTurns, () => { NextTurnClientRpc(_inTurnPlayer); });

    }

    [ClientRpc]
    private void DrawNewCardClientRpc(CardData cardData, ulong receiver, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId == receiver)
        {
            Card newCard = _cardsInDeck.Pop();
            newCard.SetData(cardData, _cardDatabase.GetCardInforSO(cardData), CardLocation.Deck);
            newCard.OnPlayCard += PlayCard;
            newCard.OnHoverCard += HoverCardInHand;
            newCard.OnExitHoverCard += (card) => { _boardCore.ClearValidSlots(); };
            newCard.OnDiscardCard += DiscardCardFromHand;
            _playerAndHolderMap[receiver].AddCard(newCard); // CardLocation will become PlayerHand inside AddCard

            // offically end turn after drawing new card
            _localPlayerState = PlayerState.NONE;
        }
        else
        {
            // draw a dummy card to represent this action
            Card dummyCard = _cardsInDeck.Pop();
            _playerAndHolderMap[receiver].AddCard(dummyCard);
        }
    }

    [ClientRpc]
    private void NextTurnClientRpc(ulong nextPlayerId)
    {
        _inTurnPlayer = nextPlayerId;
        if (NetworkManager.Singleton.LocalClientId == nextPlayerId)
        {
            // it's your turn
            Debug.Log("It's your turn!");
            // set your card holder to be active
            foreach (var holder in _cardHolders)
            {
                holder.IsTurn = holder.IsMine;
            }
        }
        else
        {
            Debug.Log($"It's player {nextPlayerId}'s turn.");
            // set your card holder to be inactive
            foreach (var holder in _cardHolders)
            {
                holder.IsTurn = false;
            }
        }
    }

    private Vector3 GetMouseWorldPointOnBoard()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (_boardPlane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            return hit;
        }
        return Vector3.zero;
    }
    #endregion

    #region Endgame step
    public void Reset()
    {
        _cardHolders.Clear();
        _playerAndHolderMap.Clear();
    }
    #endregion 
}
public enum PlayerState
{
    NONE,
    PLACING_CARD,
    SELECTING_PLACE_TO_BOMB,
    USING_TOOL,
    CHECKING_GOAL,
}



