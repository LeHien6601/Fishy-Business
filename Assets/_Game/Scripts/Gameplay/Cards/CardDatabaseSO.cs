using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabaseSO", menuName = "Card/CardDatabase")]
public class CardDatabaseSO : ScriptableObject
{
    [SerializeField] private List<CardInforSO> _allCardInfos = new List<CardInforSO>();

    public CardInforSO GetCardInforSO(CardData card)
    {
        return _allCardInfos[card.CardID];
    }

    public int Size() => _allCardInfos.Count;
}