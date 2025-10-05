using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Card))]
public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Card _card;
    private CardHolder _holder;
    private int _indexInHolder = -1;

    void Start()
    {
        _card = GetComponent<Card>();
        _holder = GetComponentInParent<CardHolder>();
    }

    public void SetData()
    {
        _card = GetComponent<Card>();
        _holder = GetComponentInParent<CardHolder>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_holder == null) return;
        _indexInHolder = GetMyIndex();
        if (_indexInHolder >= 0)
            _holder.SelectCard(_indexInHolder);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_holder == null || _indexInHolder < 0) return;
        _holder.UnSelectCard(_indexInHolder);
        _indexInHolder = -1;
    }

    private int GetMyIndex()
    {
        if (_holder == null) return -1;
        for (int i = 0; i < _holder.transform.childCount; i++)
        {
            Card c = _holder.transform.GetChild(i).GetComponent<Card>();
            if (c == _card) return i;
        }
        return -1;
    }
}
