using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class NetworkBoardManager : NetworkBehaviour
{
    [SerializeField] private CardDatabaseSO _cardDatabase;
    [SerializeField] private Card _cardPrefab;
    [SerializeField] private Transform _deckPlace;
    [SerializeField] private Transform _discardPile;
    [SerializeField] private Transform _turnIndicator;
    [SerializeField] private DayNightController _dayNightController;
    private GameMode _currentGameMode;
    private GameData _currentGameData;
    private BoardInteractionController _interaction;
    private readonly Stack<Card> _cardsInDeck = new(); // represents the deck of cards to be dealt
    private readonly List<CardHolder> _cardHolders = new(); // local cache of all card holders on the board, 1 is yours, the others are dummies representing other players' hands
    private readonly Dictionary<ulong, CardHolder> _playerAndHolderMap = new();
    private readonly List<CardData> _masterDeck = new(); // only server has the full deck data
    private int _tressureIndex = -1; // only server knows this, clients if want to know must send a rpc
    private GamePhase _currentPhase = GamePhase.None;
    private List<ulong> _turnOrder; // Current turn order (fixed or random per mode)
    private NetworkList<ulong> _playerOrders = new(); // sever only
    private ulong _inTurnPlayer = ulong.MaxValue;
    private const float _waitBetweenPlayerTurns = 2f;
    private const float _actionCardDuration = 1.5f;

    [SerializeField] private BoardCore _boardCore;
    private Plane _boardPlane; // for mouse raycast onto board
    private const float _placingOffset = 0.05f;
    private const float _deckStackSpace = 0.001f;
    private IEnumerator _countDownTurnRoutine;
    private int _playerStartGameCount = 0;
    public NetworkVariable<float> BoardHeight = new NetworkVariable<float>();


    // API - transfer visual effects/ sound effects to another class to handle
    public event UnityAction<Vector3> BombEvent = delegate { };
    // public event UnityAction 

    private void Awake()
    {
        _interaction = new BoardInteractionController(this);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            BoardHeight.Value = transform.position.y;
        }
        _boardPlane = new Plane(Vector3.up, new Vector3(0, BoardHeight.Value, 0));
    }

    #region SETUP
    public async void ServerStartGameLogic(NetworkList<ulong> playerOrders)
    {
        if (!IsServer) return;
        _playerOrders = playerOrders;

        // Perform server-only setup
        InitializeAndShuffleDeck();

        // Calculate count of cat and dog
        AssignRoles(playerOrders.Count);

        // random goal tressure
        _tressureIndex = Random.Range(0, 3);

        _currentGameData = JsonUtility.FromJson<GameData>(LobbyManager.Instance.currentLobby.Data[Constant.KEY_GAME_MODE_DATA].Value);
        _currentGameMode = GameConfig.Instance.GameModes[_currentGameData.GameModeIndex];
        _currentGameMode.Initialize(this, playerOrders);
        await Task.Delay(1000); // wait for a moment to ensure all clients are ready
        _playerStartGameCount = 0;
        StartGameClientRpc();
        while (_playerStartGameCount < NetworkManager.Singleton.ConnectedClients.Count)
        {
            await Task.Yield();
        }
        GameplayManager.Instance.HandleStartGame(_playerAndHolderMap.ToDictionary(
            kvp => kvp.Key, kvp => kvp.Value.PlayerRole));

        DealCards(playerOrders);
        await Task.Delay(1000); // wait for a moment before starting first turn
        _currentGameMode.StartGame(this);
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("START GAME ON CLIENT");
        _currentGameData = JsonUtility.FromJson<GameData>(LobbyManager.Instance.currentLobby.Data[Constant.KEY_GAME_MODE_DATA].Value);
        _turnIndicator.gameObject.SetActive(_currentGameData.GameModeIndex == 0); // classic mode has turn indicator, others don't
        // spawn board, facing towards local player
        _boardCore.GenerateBoard();
        if (_playerAndHolderMap.TryGetValue(NetworkManager.Singleton.LocalClientId, out CardHolder _))
        {
            Vector3 direction = transform.position - _playerAndHolderMap[NetworkManager.Singleton.LocalClientId].transform.position;
            direction.y = 0;
            transform.rotation = Quaternion.LookRotation(direction);
        }
        // spawn deck,
        _cardsInDeck.Clear();

        for (int i = 0; i < _cardDatabase.TotalCount(); i++)
        {
            var card = Instantiate(_cardPrefab, _deckPlace.position + _deckStackSpace * i * Vector3.up, Quaternion.identity, _deckPlace);
            card.transform.localRotation = Quaternion.identity;
            _cardsInDeck.Push(card);
        }
        Debug.Log("START GAME ON CLIENT END");
        CountPlayerStartGameRpc();
    }
    [Rpc(SendTo.Server)]
    private void CountPlayerStartGameRpc()
    {
        _playerStartGameCount++;
    }

    public void SetTurnOrder(List<ulong> order) => _turnOrder = order;
    public void SetCurrentPhase(GamePhase phase) => _currentPhase = phase;
    public void StartNightPhase()
    {
        if (_turnOrder == null || _turnOrder.Count == 0) return;
        _inTurnPlayer = _turnOrder[0];
        _currentGameMode.HandlePlayerTurnStart(this, _inTurnPlayer);
    }
    public void StartNextTurn()
    {
        if (_turnOrder == null || _turnOrder.Count == 0) return;
        _inTurnPlayer = _turnOrder[(_turnOrder.IndexOf(_inTurnPlayer) + 1) % _turnOrder.Count]; // Or mode-specific index
        _currentGameMode.HandlePlayerTurnStart(this, _inTurnPlayer);
    }

    private void DealCards(NetworkList<ulong> playerOrders)
    {
        if (!IsServer) return;

        int currentDeckIndex = 0;

        int CardsPerPlayer = CalculateCardsPerPlayer(playerOrders.Count);
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            List<CardData> hand = new();
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
        WaitForSeconds wait = Utils.GetWaitForSeconds(0.1f);
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
            Debug.Log("resgistered client: " + clientId);
            _cardHolders.Add(holder);
            _playerAndHolderMap[clientId] = holder;
        }
    }

    private void AssignRoles(int totalPlayer)
    {
        if (!IsServer) return;

        // Define number of cats based on total players
        var catCount = totalPlayer switch
        {
            1 => 0,
            2 or 3 or 4 => 1,
            5 or 6 => 2,
            7 or 8 or 9 => 3,
            10 => 4,
            _ => 10
        };

        var remainingCats = catCount;
        // Randomly assign roles to players
        foreach (var player in _playerOrders)
        {
            PlayerRole role = remainingCats > 0 && Random.Range(0, _playerOrders.Count) < remainingCats
                ? PlayerRole.Cat
                : PlayerRole.Dog;

            if (role == PlayerRole.Cat)
                remainingCats--;

            _playerAndHolderMap[player].SetRole(role); // set on server
            SetRoleClientRpc(role, player, new() // set on clients
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { player }
                }
            });
        }
    }

    [ClientRpc]
    private void SetRoleClientRpc(PlayerRole arg0, ulong senderId, ClientRpcParams clientRpcParams)
    {
        CardHolder cardHolder = _playerAndHolderMap[senderId];
        cardHolder.SetRole(arg0);
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
        _interaction.BeginCardInteraction(card);
        card.Holder.RemoveCard(card);
        card.transform.SetParent(_boardCore.transform);
        _boardCore.DropCardOntoBoard(card, onComplete: () => _interaction.EnterStateForCard(card));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
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
        if (!_playerAndHolderMap.TryGetValue(senderId, out CardHolder _)) return;
        CardHolder cardHolder = _playerAndHolderMap[senderId];
        Card card = cardHolder.RemoveRandomCard();
        _interaction.BeginCardInteraction(card);
        card.transform.SetParent(_boardCore.transform);
        card.SetData(_cardDatabase.GetCardInforSO(arg0), CardLocation.OnBoard);

        _boardCore.DropCardOntoBoard(card);
    }

    #endregion

    #region HOVER CARD ON BOARD
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void MovePlacingCardServerRpc(Vector2Int slot, Quaternion quaternion)
    {
        MovePlacingCardClientRpc(slot, quaternion);
    }

    [ClientRpc]
    private void MovePlacingCardClientRpc(Vector2Int slot, Quaternion quaternion)
    {
        if (_interaction.PlacingCard.CardType == CardType.Path)
        {
            _interaction.PlacingCard.transform.localRotation = quaternion;
            _interaction.HoveringSlot = slot;
            if (!_boardCore.IsPlacableWithCurrentRotation(_interaction.PlacingCard, slot))
            {
                _interaction.PlacingCard.Rotate();
            }
        }
        else if (_interaction.PlacingCard.ActionCardType != ActionCardType.Bomb &&
                 _interaction.PlacingCard.ActionCardType != ActionCardType.CheckGold &&
                 _interaction.PlacingCard.ActionCardType != ActionCardType.SwapGoal)
        {
            return;
        }

        // move card visually to this slot position (slightly above)
        Vector3 targetPos = _boardCore.GetWorldPositionForSlot(slot) + Vector3.up * _placingOffset;
        _interaction.PlacingCard.transform.DOMove(targetPos, 0.04f).SetEase(Ease.OutQuad);
    }
    #endregion

    #region HANDLE INPUT AFTER PLAYING A CARD TO BOARD
    private void Update()
    {
        _interaction.HandleUpdate();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ConfirmCardPlacementServerRpc(Vector2Int slot, Quaternion rot)
    {
        ConfirmCardPlacementClientRpc(slot, rot);
    }

    [ClientRpc]
    private void ConfirmCardPlacementClientRpc(Vector2Int slot, Quaternion rot)
    {
        if (_interaction.PlacingCard)
        {
            _boardCore.PlacePathCardAt(_interaction.PlacingCard, slot, rot, () =>
            {
                ServerCheckConnectingToHiddenGoals();
            });
            _boardCore.ClearValidSlots();
            _interaction.ClearPlacement();
        }
        else
        {
            Debug.LogWarning("_placingCard should not be null in this function");
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RotateCardServerRpc()
    {
        RotateCardClientRpc();
    }


    [ClientRpc]
    private void RotateCardClientRpc()
    {
        if (_interaction.PlacingCard)
        {
            _interaction.PlacingCard.Rotate();
        }
        else
        {
            Debug.LogWarning("_placingCard should not be null in this function");
        }
    }

    private void ServerCheckConnectingToHiddenGoals()
    {
        if (!IsServer)
            return;
        List<GoalTracer> tracers = _boardCore.GetPathsToHiddenGoals();
        bool isEndGame = false;
        foreach (var tracer in tracers) // 3 tracers at most
        {
            bool isTreasure = tracer.GoalSlot.x / 2 == _tressureIndex;

            if (isTreasure)
            {
                isEndGame = true;
                foreach (ulong playerId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    // if is Cat
                    if (_playerOrders.Contains(playerId) && _playerAndHolderMap[playerId].PlayerRole == PlayerRole.Cat)
                    {
                        RevealGoalCardClientRpc(tracer, isDog: false, isTreasure, new() // set on clients
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new[] { playerId }
                            }
                        });

                    }
                    else
                    {
                        RevealGoalCardClientRpc(tracer, isDog: true, isTreasure, new() // set on clients
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new[] { playerId }
                            }
                        });
                    }
                }
            }
            else
            {
                RevealGoalCardClientRpc(tracer, isTreasure);
            }
        }
        if (isEndGame)
        {
            _currentGameMode.EndGame(this, isDogWin: true);
            // ServerEndBoardGame(isDogWin: true);
        }
        else
        {
            Debug.Log("Game continue");
            ServerDrawNewCardThenEndTurn();
        }
    }
    [ClientRpc]
    private void RevealGoalCardClientRpc(GoalTracer tracer, bool isDog, bool isTreasure, ClientRpcParams clientRpcParams)
    {
        _boardCore.OpenHiddenGoalCard(tracer, isTreasure, isDog);
        if (isTreasure)
        {
            foreach (var holder in _cardHolders)
            {
                holder.IsTurn = false;
            }
        }
    }

    [ClientRpc]
    private void RevealGoalCardClientRpc(GoalTracer tracer, bool isTreasure)
    {
        _boardCore.OpenHiddenGoalCard(tracer, isTreasure);
        if (isTreasure)
        {
            foreach (var holder in _cardHolders)
            {
                holder.IsTurn = false;
            }
        }
    }
    #endregion

    #region DISCARD CARD FROM HAND T0 DISCARD PILE
    private void DiscardCardFromHand(Card card)
    {
        card.Holder.IsTurn = false;
        card.Holder.RemoveCard(card);
        card.transform.SetParent(_discardPile.transform);
        card.transform.DOScale(1f, 1f).SetEase(Ease.OutCubic);
        card.transform.DOMove(_discardPile.transform.position + _discardPile.transform.childCount * _deckStackSpace * Vector3.up, 1f).SetEase(Ease.OutCubic);
        card.transform.DOLocalRotate(Vector3.zero, 1f).SetEase(Ease.OutCubic);
        // zero because we already edit the _discardPile pos and rot
        DiscardCardFromHandServerRpc(senderId: NetworkManager.Singleton.LocalClientId);
        _boardCore.ClearValidSlots();

    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DiscardCardFromHandServerRpc(ulong senderId)
    {
        Debug.Log("Discard server");
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
        StopCountDown();
        CardHolder cardHolder = _playerAndHolderMap[senderId];
        Card card = cardHolder.RemoveRandomCard();
        card.transform.SetParent(_discardPile.transform);
        card.transform.DOScale(1f, 1f).SetEase(Ease.OutCubic);
        card.transform.DOMove(_discardPile.transform.position + _discardPile.transform.childCount * _deckStackSpace * Vector3.up, 1f).SetEase(Ease.OutCubic);
        card.transform.DOLocalRotate(Vector3.zero, 1f).SetEase(Ease.OutCubic);
        // zero because we already edit the _discardPile pos and rot
    }

    #endregion

    #region  SWAP GOALS
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SwapGoalsServerRpc(Vector2Int slotA, Vector2Int slotB, ulong senderId)
    {
        if (_tressureIndex == slotA.x / 2)
        {
            _tressureIndex = slotB.x / 2;
        }
        else if (_tressureIndex == slotB.x / 2)
        {
            _tressureIndex = slotA.x / 2;
        }
        SwapGoalsClientRpc(slotA, slotB, senderId);
        this.WaitThenExecute(_actionCardDuration, () => ServerDrawNewCardThenEndTurn());
    }

    [ClientRpc]
    private void SwapGoalsClientRpc(Vector2Int slotA, Vector2Int slotB, ulong senderId)
    {
        _interaction.PlacingCard.transform.DOMove(_boardCore.GetWorldPositionForSlot(slotA), BoardCore.PlaceToSlotDuration).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(_interaction.PlacingCard.gameObject);
            _interaction.ClearCardAndTarget();
        });
        _boardCore.SwapGoals(slotA, slotB);
        // GameplayManager.Instance.TriggerActionCard(ActionCardType.SwapGoal, ToolType.None);
        if (_currentGameMode is ClassicGameMode || NetworkManager.Singleton.LocalClientId == senderId)
        {
            SoundManager.Play2D(SoundType.SwapGoal);
        }
    }
    #endregion


    #region CHECK GOAL
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CheckThisGoalServerRpc(ulong requesterId, Vector2Int checkSlot)
    {
        // goal row = 0 2 4, divided by 2 is 0 1 2, exactly the indexes we want
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
        _interaction.PlacingCard.transform.DOMove(_boardCore.GetWorldPositionForSlot(checkSlot), BoardCore.PlaceToSlotDuration).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(_interaction.PlacingCard.gameObject);
            _interaction.ClearCardAndTarget();
        });
        CardHolder requester = _playerAndHolderMap[requesterId];
        _boardCore.ShowThisGoalCard(checkSlot,
                                    showTarget: requester.BeforeFaceSlot(),
                                    isTressure: checkResult,
                                    revealCardData: NetworkManager.Singleton.LocalClientId == requesterId);
        GameplayManager.Instance.TriggerActionCard(ActionCardType.CheckGold, ToolType.None, requesterId, requesterId, _currentGameMode is ClassicGameMode);
    }
    #endregion

    #region BOMB A PATH ON BOARD
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void BombThisPathServerRpc(Vector2Int slot, ulong senderId)
    {
        if (!_boardCore.OnBoardPaths.Contains(slot))
        {
            Debug.LogWarning("this slot is not in OnBoardPaths list");
            return;
        }
        BombThisPathClientRpc(slot, senderId);
        this.WaitThenExecute(_actionCardDuration, () => ServerDrawNewCardThenEndTurn());
    }

    [ClientRpc]
    private void BombThisPathClientRpc(Vector2Int slot, ulong senderId)
    {
        Vector3 wp = _boardCore.GetWorldPositionForSlot(slot);
        _interaction.PlacingCard.transform.DOMove(wp, BoardCore.PlaceToSlotDuration).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(_interaction.PlacingCard.gameObject);
            _interaction.ClearCardAndTarget();
        });
        BombEvent.Invoke(wp);
        _boardCore.BombThisPath(slot);
        // @TODO: add some visuals
        GameplayManager.Instance.TriggerActionCard(ActionCardType.Bomb, ToolType.None, senderId, senderId, _currentGameMode is ClassicGameMode);
    }

    #endregion

    #region TARGET A PLAYER WITH ACTION CARD
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SwitchTargerPlayerServerRpc(ulong targetId)
    {
        SwitchTargerPlayerClientRpc(targetId);
    }

    [ClientRpc]
    private void SwitchTargerPlayerClientRpc(ulong targetId)
    {
        _interaction.TargetPlayer = targetId;
        Transform targetTf = _playerAndHolderMap[targetId].BeforeFaceSlot();
        _interaction.PlacingCard.transform.DOMove(targetTf.position, 0.1f);
        _interaction.PlacingCard.transform.DORotateQuaternion(targetTf.rotation, 0.1f);
        Debug.Log("targeting player " + targetId);
    }
    #endregion

    #region TOOL FUNCTION ON A PLAYER

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ApplyActionCardOnPlayerServerRpc(ulong targetPlayer, ulong senderId)
    {
        switch (_interaction.PlacingCard.ActionCardType)
        {
            case ActionCardType.BrokenTool:
            case ActionCardType.FixTool:
                ApplyToolOnPlayerClientRpc(targetPlayer, senderId);
                break;
            case ActionCardType.Binoculars:
                ApplyBinocularsToPlayerClientRpc(targetPlayer);
                break;
            case ActionCardType.Shield:
                ApplyShieldToPlayerClientRpc(targetPlayer, senderId);
                break;
            case ActionCardType.SwapGoal:
                break;
            default:
                Debug.LogWarning("Only tool cards that target players are handled here.");
                return;
        }
        DOVirtual.DelayedCall(_actionCardDuration, () => ServerDrawNewCardThenEndTurn());
        // this.WaitThenExecute(_actionCardDuration, () => ServerDrawNewCardThenEndTurn());
        // if (_placingCard.ActionCardType == ActionCardType.BrokenTool || _placingCard.ActionCardType == ActionCardType.FixTool)
        // {
        //     ApplyToolOnPlayerClientRpc(targetPlayer);
        //     this.WaitThenExecute(_actionCardDuration, () => ServerDrawNewCardThenEndTurn());
        // }
    }

    [ClientRpc]
    private void ApplyToolOnPlayerClientRpc(ulong tagetPlayer, ulong senderId)
    {
        CardHolder holder = _playerAndHolderMap[tagetPlayer];
        if (_interaction.PlacingCard.ActionCardType == ActionCardType.BrokenTool && holder.Shield)
        {
            holder.Shield = false;
            Debug.Log("Your shield has blocked an attack from somebody");
            //@TODO: notify has shield
            if (NetworkManager.Singleton.LocalClientId == tagetPlayer || NetworkManager.Singleton.LocalClientId == senderId)
            {
                ShowEffectShieldBreak();
            }
        }
        else
        {
            holder.SetTool(_interaction.PlacingCard.ToolType, _interaction.PlacingCard.ActionCardType == ActionCardType.FixTool, tagetPlayer, senderId, _currentGameMode is ClassicGameMode);

        }
        Destroy(_interaction.PlacingCard.gameObject);
        _interaction.ClearCardAndTarget();
    }
    private void ShowEffectShieldBreak()
    {
        GameplayManager.Instance.TriggerShieldBreak();
    }

    [ClientRpc]
    private void ApplyBinocularsToPlayerClientRpc(ulong targetPlayerId, ClientRpcParams clientRpcParams = default)
    {
        CardHolder holder = _playerAndHolderMap[targetPlayerId];
        holder.NightVision = true;
        Destroy(_interaction.PlacingCard.gameObject);
        _interaction.ClearCardAndTarget();
    }
    [ClientRpc]
    private void ApplyShieldToPlayerClientRpc(ulong targetPlayerId, ulong senderId, ClientRpcParams clientRpcParams = default)
    {
        CardHolder holder = _playerAndHolderMap[targetPlayerId];
        holder.Shield = true;
        Destroy(_interaction.PlacingCard.gameObject);
        _interaction.ClearCardAndTarget();
        GameplayManager.Instance.TriggerActionCard(ActionCardType.Shield, ToolType.None, targetPlayerId, senderId, _currentGameMode is ClassicGameMode);

        if (_currentGameMode is ClassicGameMode || NetworkManager.Singleton.LocalClientId == targetPlayerId || NetworkManager.Singleton.LocalClientId == senderId)
        {
            SoundManager.Play2D(SoundType.ShieldApply);
        }

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
        StopCountDown();
        int id = _masterDeck.Count - _cardsInDeck.Count;
        if (id >= 0 && id < _masterDeck.Count) // check before sending RPC to save bandwidth
        {
            DrawNewCardClientRpc(_masterDeck[id], receiver: _inTurnPlayer);
        }
        // wait for a short moment then end turn
        // _inTurnPlayer = _turnOrder[(_turnOrder.IndexOf(_inTurnPlayer) + 1) % _turnOrder.Count];
        this.WaitThenExecute(_waitBetweenPlayerTurns, () =>
        {
            if (_currentGameMode.CheckEndGameConditions(this, out bool isDogWin))
            {
                _currentGameMode.EndGame(this, isDogWin);
            }
            _currentGameMode.HandlePlayerActionComplete(this);
        });
    }

    [ClientRpc]
    private void DrawNewCardClientRpc(CardData cardData, ulong receiver, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId == receiver)
        {
            StopCountDown();
            Card newCard = _cardsInDeck.Pop();
            newCard.SetData(cardData, _cardDatabase.GetCardInforSO(cardData), CardLocation.Deck);
            newCard.OnPlayCard += PlayCard;
            newCard.OnHoverCard += HoverCardInHand;
            newCard.OnExitHoverCard += (card) => { _boardCore.ClearValidSlots(); };
            newCard.OnDiscardCard += DiscardCardFromHand;
            _playerAndHolderMap[receiver].AddCard(newCard); // CardLocation will become PlayerHand inside AddCard

            // offically end turn after drawing new card
            _interaction.SetIdle();
        }
        else
        {
            // draw a dummy card to represent this action
            Card dummyCard = _cardsInDeck.Pop();
            _playerAndHolderMap[receiver].AddCard(dummyCard);
        }
    }

    [ClientRpc]
    private void NextTurnClientRpc(ulong nextPlayerId, int turnNumber)
    {
        // _inTurnPlayer = nextPlayerId;
        if (!_playerAndHolderMap.TryGetValue(nextPlayerId, out CardHolder _)) return;
        CardHolder inTurnHolder = _playerAndHolderMap[nextPlayerId];
        if (NetworkManager.Singleton.LocalClientId == nextPlayerId)
        {
            Debug.Log("My turn");
            inTurnHolder.IsTurn = true;
            StartCountDown();
        }
        else
        {
            Debug.Log($"It's player {nextPlayerId}'s turn.");
            foreach (var holder in _cardHolders)
            {
                holder.IsTurn = false;
            }
        }

        // ---- visual -----
        var direction = inTurnHolder.transform.position - _turnIndicator.position;
        direction.y = 0;
        _turnIndicator.DORotateQuaternion(Quaternion.LookRotation(direction), 0.1f);

        GameplayManager.Instance.HandleNewTurn(nextPlayerId, turnNumber);
    }

    internal Vector3 GetMouseWorldPointOnBoard()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.value);
        if (_boardPlane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            return hit;
        }
        return Vector3.zero;
    }

    internal BoardCore BoardCore => _boardCore;
    internal Dictionary<ulong, CardHolder> PlayerAndHolderMap => _playerAndHolderMap;
    internal void RequestMovePlacingCard(Vector2Int slot, Quaternion rotation) => MovePlacingCardServerRpc(slot, rotation);
    internal void RequestConfirmPlacement(Vector2Int slot, Quaternion rotation) => ConfirmCardPlacementServerRpc(slot, rotation);
    internal void RequestRotatePlacingCard() => RotateCardServerRpc();
    internal void RequestBombPath(Vector2Int slot, ulong senderId) => BombThisPathServerRpc(slot, senderId);
    internal void RequestApplyActionToTarget(ulong targetPlayer, ulong senderId) => ApplyActionCardOnPlayerServerRpc(targetPlayer, senderId);
    internal void RequestCheckGoal(ulong requesterId, Vector2Int slot) => CheckThisGoalServerRpc(requesterId, slot);
    internal void RequestSwapGoals(Vector2Int slotA, Vector2Int slotB, ulong senderId) => SwapGoalsServerRpc(slotA, slotB, senderId);
    internal void RequestSwitchTargetPlayer(ulong targetId) => SwitchTargerPlayerServerRpc(targetId);
    #endregion

    #region AutoPlay
    private void StopCountDown()
    {
        if (_countDownTurnRoutine != null)
        {
            StopCoroutine(_countDownTurnRoutine);
            _countDownTurnRoutine = null;
        }
    }

    private void StartCountDown()
    {
        if (_countDownTurnRoutine != null)
        {
            StopCoroutine(_countDownTurnRoutine);
        }
        _countDownTurnRoutine = CountDownTurnRoutine();
        StartCoroutine(_countDownTurnRoutine);
    }


    private void OnEndTime() // only the in-turn player is supposed to call this function
    {
        if (_playerAndHolderMap == null || _playerAndHolderMap.Count <= 0)
        {
            return;
        }
        Debug.Log("End time");
        if (_interaction.PlacingCard)
        {
            _interaction.SetIdle();
            DiscardPlacingCardServerRpc();
        }
        else
        {
            CardHolder inTurnHolder = _playerAndHolderMap[_inTurnPlayer];
            if (inTurnHolder == null) return;
            inTurnHolder.DiscardRandomCard();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DiscardPlacingCardServerRpc()
    {
        Debug.Log("Discard");
        ServerDrawNewCardThenEndTurn();
        DiscardPlacingCardClientRpc();
    }

    [ClientRpc]
    private void DiscardPlacingCardClientRpc()
    {
        _interaction.SetIdle();
        Card placingCard = _interaction.PlacingCard;
        _interaction.ClearPlacement();
        placingCard.transform.SetParent(_discardPile.transform);
        placingCard.transform.DOScale(1f, 1f).SetEase(Ease.OutCubic);
        placingCard.transform.DOMove(_discardPile.transform.position + _discardPile.transform.childCount * _deckStackSpace * Vector3.up, 1f).SetEase(Ease.OutCubic);
        placingCard.transform.DOLocalRotate(Vector3.zero, 1f).SetEase(Ease.OutCubic);
    }

    private IEnumerator CountDownTurnRoutine()
    {
        float time = _currentGameData.TurnInterval;
        while (time > 0)
        {
            time -= Time.deltaTime;
            yield return null;
        }
        OnEndTime();
    }
    #endregion

    #region Endgame step

    private void ServerEndBoardGame(bool isDogWin)
    {
        if (!IsServer)
            return;

        List<ulong> winners = new();
        List<ulong> players = new();
        PlayerRole roleWin = isDogWin ? PlayerRole.Dog : PlayerRole.Cat;
        foreach (var pair in _playerAndHolderMap)
        {
            if (pair.Value.PlayerRole == roleWin)
            {
                winners.Add(pair.Key);
            }
            players.Add(pair.Key);
        }
        GameplayManager.Instance.HandleEndGame(winners, players);
    }

    public void Reset()
    {
        List<ulong> idList = new();
        foreach (var player in _playerOrders)
        {
            idList.Add(player);
        }
        GameplayManager.Instance.HandleResetGame(idList);
        StopAllCoroutines();
        ResetClientRpc();
        _playerOrders.Clear();
        _dayNightController.Uncover();
    }

    [ClientRpc]
    private void ResetClientRpc()
    {
        foreach (var holder in _cardHolders)
        {
            Destroy(holder.gameObject);
        }
        StopAllCoroutines();
        _turnIndicator.gameObject.SetActive(false);
        _cardHolders.Clear();
        _playerAndHolderMap.Clear();
        _interaction.Reset();
        _deckPlace.DeleteChildren();
        _discardPile.DeleteChildren();
        _boardCore.transform.DeleteChildren();
        _dayNightController.Uncover();
    }
    #endregion 

    #region GETTERS
    public PlayerRole GetPlayerRole()
    {
        PlayerRole role = PlayerRole.Unknown;
        foreach (var pair in _playerAndHolderMap)
        {
            if (pair.Key == NetworkManager.Singleton.LocalClientId) return pair.Value.PlayerRole;
        }
        return role;
    }
    public NetworkList<ulong> GetPlayerOrders() => _playerOrders;
    #endregion

    public void RequestNextTurn(ulong playerId)
    {
        _inTurnPlayer = playerId;
        NextTurnClientRpc(playerId, _turnOrder.IndexOf(playerId));
    }
    public bool CheckForOutOfCards()
    {
        bool isOutOfCards = true;
        foreach (var holder in _cardHolders)
        {
            if (!holder.IsEmpty())
            {
                isOutOfCards = false;
                break;
            }
        }
        return isOutOfCards;
    }

    public void BroadcastGameEnd(bool dogsWin)
    {
        ServerEndBoardGame(dogsWin);
    }





    #region Phased Game Functions
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void CoverAllClientsRpc()
    {
        CardHolder localHolder = _playerAndHolderMap[NetworkManager.Singleton.LocalClientId];
        // if (localHolder.NightVision)
        // {
        //     return; // do not cover if has night vision
        // }
        _dayNightController.Cover(localHolder.NightVision);
    }


    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void UncoverAllClientsRpc()
    {
        CardHolder localHolder = _playerAndHolderMap[NetworkManager.Singleton.LocalClientId];
        localHolder.NightVision = false; // reset night vision after uncover
        _dayNightController.Uncover();
    }

    [ClientRpc]
    public void UnCoverASpecificClientRpc(ClientRpcParams clientRpcParams)
    {
        _dayNightController.Uncover();
    }

    [ClientRpc]
    public void CoverASpecificClientRpc(ClientRpcParams clientRpcParams)
    {
        _dayNightController.Cover();
    }

    #endregion
}
