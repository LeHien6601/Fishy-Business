using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabaseSO", menuName = "Card/CardDatabase")]
public class CardDatabaseSO : ScriptableObject
{
    // will discard this _allCardInfos in production
    [SerializeField] private List<CardInforSO> _allCardInfos = new List<CardInforSO>();
    [SerializeField] private List<CardPack> _decks;
    private int _totalCount = -1;

    public CardInforSO GetCardInforSO(CardData card)
    {
        return _decks[card.CardID].CardInforSO;
    }
    public int TotalCount()
    {
        if (_totalCount < 0)
        {
            _totalCount = 0;
            foreach (var c in _decks)
            {
                _totalCount += c.Amount;
            }
        }
        return _totalCount;
    }

    public int GetCopiesOfCard(int id) => _decks[id].Amount;
    public int Size() => _decks.Count;


    // [ContextMenu("Add All Cards To Deck")]
    // public void a()
    // {
    //     foreach (var card in _allCardInfos)
    //     {
    //         _decks.Add(new CardPack(card, 1));
    //     }
    // }
}

[System.Serializable]
public struct CardPack
{
    public CardInforSO CardInforSO;
    public int Amount;

    public CardPack(CardInforSO cardInforSO, int amount)
    {
        CardInforSO = cardInforSO;
        Amount = amount;
    }
}