using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class NetworkBoardManager : NetworkBehaviour
{
    [SerializeField] private CardDatabaseSO _cardDatabase;
    [SerializeField] private Card _cardPrefab;
    [SerializeField] private Transform _deckPlace;
    [SerializeField] private float _deckStackSpace = 0.002f;
    private readonly Stack<Card> _freshCards = new(); // represents the deck of cards to be dealt
    private readonly List<CardHolder> _cardHolders = new(); // local cache of all card holders on the board, 1 is yours, the others are dummies representing other players' hands
    private readonly Dictionary<ulong, CardHolder> _map = new();
    private readonly List<CardData> _masterDeck = new(); // only server has the full deck data
    private NetworkList<ulong> _playerOrders = new();

    [SerializeField] private BoardCore _boardCore;
    private Plane _boardPlane; // for mouse raycast onto board
    private Vector2Int? _hoveringSlot = null;
    private float _snapDistance = 1.5f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _boardPlane = new Plane(Vector3.up, transform.position);
    }


    #region  Setup logic
    public void StartGameLogic(NetworkList<ulong> playerOrders)
    {
        if (!IsServer) return;

        // Perform server-only setup
        _playerOrders = playerOrders;
        InitializeAndShuffleDeck();
        StartGameClientRpc();
        DealCards(playerOrders);
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        // spawn board, facing towards local player

        // spawn deck,
        _freshCards.Clear();
        for (int i = 0; i < _cardDatabase.Size(); i++)
        {
            _freshCards.Push(Instantiate(_cardPrefab, _deckPlace.position + _deckStackSpace * i * Vector3.up, _cardPrefab.transform.rotation, _deckPlace));
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
                Card newCard = _freshCards.Pop();
                if (holder.IsMine)
                {
                    newCard.SetData(cardData, _cardDatabase.GetCardInforSO(cardData), CardLocation.Deck);
                    newCard.OnPlayCard += PlayCard;
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
            _masterDeck.Add(new CardData { CardID = i });
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
            _cardHolders.Add(holder);
            _map[clientId] = holder;
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


    #region  Boardgame logic
    // @TODO:

    private void PlayCard(Card arg0)
    {
        PlayCardServerRpc(arg0.CardData, NetworkManager.Singleton.LocalClientId);
        arg0.Holder.RemoveCard(arg0);
        var validSlots = _boardCore.GetValidSlotsForCard(arg0);
        if (validSlots.Count > 0)
        {
            _boardCore.HoverCardAt(arg0, validSlots[0]);
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
        PlayCardClientRpc(arg0, senderId, clientRpcParams);
    }

    [ClientRpc]
    private void PlayCardClientRpc(CardData arg0, ulong senderId, ClientRpcParams clientRpcParams)
    {
        CardHolder cardHolder = _map[senderId];
        Card card = cardHolder.RemoveRandomCard();
        card.SetData(_cardDatabase.GetCardInforSO(arg0), CardLocation.OnBoard);
        var validSlots = _boardCore.GetValidSlotsForCard(card);
        if (validSlots.Count > 0)
        {
            _boardCore.HoverCardAt(card, validSlots[0]);
        }
    }

    private void MovePlacingCard()
    {
        Vector3 mouseWorld = GetMouseWorldPointOnBoard();
        if (mouseWorld == Vector3.zero)
            return;

        float bestDist = float.MaxValue;
        Vector2Int? best = null;
        foreach (var s in _boardCore.ValidSlots)
        {
            Vector3 wp = _boardCore.GetWorldPositionForSlot(s);
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
            if (!_hoveringSlot.HasValue || _hoveringSlot.Value != best.Value)
            {
                // update rotation to best fit (auto)
                // Vector3 angle = GetBestVector3RotationForCard(_placingCard, best.Value);
                // _placingCard.transform.DOLocalRotate(angle, 0.06f).SetEase(Ease.OutQuad);
                // _manualRotated = IsCardFlipped();
                // _placingCard.SetConnectionByRotate(_manualRotated);
                _hoveringSlot = best.Value;
            }

            // move card visually to this slot position (slightly above)
            // Vector3 targetPos = GetWorldPositionForSlot(best.Value) + Vector3.up * _placingOffset;
            // _placingCard.transform.DOMove(targetPos, 0.04f).SetEase(Ease.OutQuad);
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


    #region  Endgame step
    public void Reset()
    {
        _cardHolders.Clear();
        _map.Clear();
    }
    #endregion 
}
