using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class NetworkBoardManager : NetworkBehaviour
{
    [SerializeField] private CardDatabaseSO _cardDatabase;
    [SerializeField] private Card _cardPrefab;
    private readonly List<CardHolder> _cardHolders = new(); // local cache of all card holders on the board, 1 is yours, the others are dummies representing other players' hands
    private readonly List<CardData> _masterDeck = new();

    public void StartGameLogic(NetworkList<ulong> playerOrders)
    {
        if (!IsServer) return;

        // Perform server-only setup
        InitializeAndShuffleDeck();
        DealCards(playerOrders);
    }

    #region  Setup logic server only
    private void DealCards(NetworkList<ulong> playerOrders)
    {
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
                Card newCard = Instantiate(_cardPrefab, transform.position, Quaternion.identity);
                if (holder.IsMine)
                {
                    newCard.SetData(_cardDatabase.GetCardInforSO(cardData), CardLocation.Deck);
                }
                holder.AddCard(newCard); // CardLocation will become PlayerHand inside AddCard
                Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} added card {cardData.CardID} to holder.");
                yield return wait;
            }
        }
    }

    private void InitializeAndShuffleDeck()
    {
        // Example: Populate the master deck with card data
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

    public void RegisterCardHolder(CardHolder holder)
    {
        if (!_cardHolders.Contains(holder))
        {
            _cardHolders.Add(holder);
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
    #endregion


    #region  Endgame step
    public void Reset()
    {
        _cardHolders.Clear();
    }
    #endregion 
}
