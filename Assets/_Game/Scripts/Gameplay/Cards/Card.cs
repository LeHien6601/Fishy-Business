using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Unity.Collections;
using System;
using UnityEngine.Events;
using NUnit.Framework;

[SelectionBase]
public class Card : MonoBehaviour
{
    [SerializeField] private CardInforSO _cardInforSO;
    public CardData CardData { get; private set; }
    [SerializeField] private MeshRenderer _meshRenderer;

    [Header("Card State")]
    public CardLocation Location = CardLocation.None;

    [ReadOnly] public CardType CardType;
    [ReadOnly] public PathCardType PathCardType;
    [ReadOnly] public ActionCardType ActionCardType;
    [ReadOnly] public ToolType ToolType;
    [ReadOnly] public bool[] Connections;
    public CardInforSO CardInforSO => _cardInforSO;

    [SerializeField] private CardHolder _holder;
    public CardHolder Holder
    {
        get => _holder;
        set
        {
            _holder = value;
        }
    }

    public event UnityAction<Card, CardHolder> OnClickCard;
    public event UnityAction<Card> OnPlayCard;
    public event UnityAction<Card, CardHolder> OnDiscardCard;

    private bool _isFlipped = false;
    private Quaternion _initRotation;
    private int _indexInHolder = -1;

    void Awake()
    {
        _initRotation = transform.localRotation;
    }

    #region Set Data
    public void SetData(CardInforSO cardInforSO, CardLocation cardLocation)
    {
        _cardInforSO = cardInforSO;
        // BoardManagerRef = boardManager;
        if (cardInforSO == null) return;

        if (_meshRenderer != null)
            _meshRenderer.material = cardInforSO.material;

        _isFlipped = false;
        Location = cardLocation;
        CardType = cardInforSO.CardType;
        PathCardType = cardInforSO.PathCardType;
        ActionCardType = cardInforSO.ActionCardType;
        ToolType = cardInforSO.ToolType;
        Connections = (bool[])cardInforSO.Connections.Clone();
    }
    public void SetData(CardData cardData, CardInforSO cardInforSO, CardLocation cardLocation)
    {
        CardData = cardData;
        _cardInforSO = cardInforSO;
        if (cardInforSO == null) return;

        if (_meshRenderer != null)
            _meshRenderer.material = cardInforSO.material;

        _isFlipped = false;
        Location = cardLocation;
        CardType = cardInforSO.CardType;
        PathCardType = cardInforSO.PathCardType;
        ActionCardType = cardInforSO.ActionCardType;
        ToolType = cardInforSO.ToolType;
        Connections = (bool[])cardInforSO.Connections.Clone();
    }
    public void PlaceCard(CardInforSO cardInforSO, CardLocation cardLocation, bool isFlip)
    {
        _cardInforSO = cardInforSO;
        // BoardManagerRef = boardManager;
        if (cardInforSO == null) return;

        if (_meshRenderer != null)
            _meshRenderer.material = cardInforSO.material;

        Location = cardLocation;
        CardType = cardInforSO.CardType;
        PathCardType = cardInforSO.PathCardType;
        Rotate(isFlip);
    }
    public void PlaceCard(CardInforSO cardInforSO)
    {
        _cardInforSO = cardInforSO;
        if (cardInforSO == null) return;

        if (_meshRenderer != null)
            _meshRenderer.material = cardInforSO.material;

        Location = CardLocation.OnBoard;
        CardType = cardInforSO.CardType;
        PathCardType = cardInforSO.PathCardType;
    }
    public void SetHolder(CardHolder cardHolder)
    {
        Holder = cardHolder;
    }
    [ContextMenu("Set Material")]
    public void SetMaterial()
    {
        if (_cardInforSO != null && _meshRenderer != null)
        {
            // Reset Flip
            _isFlipped = false;
            transform.localRotation = Quaternion.Euler(90f, 0f, _initRotation.z);

            // Set new Material
            _meshRenderer.material = CardInforSO.material;

            CardType = CardInforSO.CardType;
            PathCardType = CardInforSO.PathCardType;
            Connections = (bool[])CardInforSO.Connections.Clone();
        }
    }
    public void Refresh()
    {
        _cardInforSO = null;
        _meshRenderer.material = null;
        CardType = CardType.None;
        PathCardType = PathCardType.None;
        Connections = null;
        _isFlipped = false;
        transform.localRotation = _initRotation;
    }

    public void Rotate()
    {
        if (Connections == null || Connections.Length < 4) return;
        _isFlipped = !_isFlipped;
        transform.localRotation = Quaternion.Euler(90f, _isFlipped ? 180f : 0f, 0f);

        bool[] newCon = new bool[4];
        newCon[0] = Connections[2];
        newCon[1] = Connections[3];
        newCon[2] = Connections[0];
        newCon[3] = Connections[1];
        Connections = newCon;
    }
    public void Rotate(bool isFlip)
    {
        if (CardInforSO.Connections == null || CardInforSO.Connections.Length < 4) return;
        if (CardInforSO.CardType != CardType.Path) return;
        _isFlipped = isFlip;
        bool[] temp = (bool[])CardInforSO.Connections.Clone();
        transform.localRotation = Quaternion.Euler(90f, isFlip ? 180f : 0f, 0f);

        if (_isFlipped)
        {
            bool[] newCon = new bool[4];
            newCon[0] = temp[2];
            newCon[1] = temp[3];
            newCon[2] = temp[0];
            newCon[3] = temp[1];
            Connections = newCon;
        }
        else
        {
            Connections = temp;
        }
    }
    public void SetConnectionByRotate(bool isFlip)
    {
        bool[] _origin = (bool[])CardInforSO.Connections.Clone();
        _isFlipped = isFlip;
        if (isFlip)
        {
            bool[] newCon = new bool[4];
            newCon[0] = _origin[2];
            newCon[1] = _origin[3];
            newCon[2] = _origin[0];
            newCon[3] = _origin[1];
            Connections = newCon;
        }
        else
        {
            Connections = _origin;
        }

    }
    public void SetLocation(CardLocation cardLocation)
    {
        Location = cardLocation;
    }
    #endregion

    #region Handle Hover

    public void OnMouseEnter()
    {
        if (Holder == null || !Holder.IsMine || !Holder.IsTurn) return;

        if (Location == CardLocation.PlayerHand)
        {
            if (Holder == null) return;

            Holder.SelectCard(this);
            // _indexInHolder = Holder.GetCardIndex(this);
            // if (_indexInHolder >= 0)
            //     Holder.SelectCard(_indexInHolder);
        }
        else if (Location == CardLocation.OnBoard)
        {

        }
    }
    void OnMouseExit()
    {
        if (Holder == null || !Holder.IsMine || !Holder.IsTurn) return;

        if (Location == CardLocation.PlayerHand)
        {

            if (Holder == null) return;
            Holder.UnSelectCard(this);
            // if (_indexInHolder >= 0)
            // {
            //     Holder.UnSelectCard(_indexInHolder);
            //     _indexInHolder = -1;
            // }
        }
        else if (Location == CardLocation.OnBoard)
        {

        }
    }


    #endregion
    #region Highlight (dùng để làm sáng ô có thể đặt)
    public void SetHighlight(bool on)
    {
        // if (_meshRenderer == null)
        // {
        //     // fallback: scale slightly
        //     transform.DOScale(on ? 1.05f : 1f, 0.12f).SetEase(Ease.OutQuad);
        //     return;
        // }

        // // scale for visibility
        // transform.DOScale(on ? 1.05f : 1f, 0.12f).SetEase(Ease.OutQuad);

        // // try to set emission if material hỗ trợ
        // if (_meshRenderer.material.HasProperty("_EmissionColor"))
        // {
        //     Color baseCol = _meshRenderer.material.HasProperty("_Color") ? _meshRenderer.material.color : Color.white;
        //     Color target = on ? baseCol * 1.6f : baseCol * 1.0f;
        //     DOTween.To(() => _meshRenderer.material.GetColor("_EmissionColor"),
        //                x => _meshRenderer.material.SetColor("_EmissionColor", x),
        //                target, 0.12f);
        // }
    }
    #endregion
    #region Handle Card on Board

    #endregion

    #region Handle Click Card
    void OnMouseDown()
    {
        if (Holder == null || !Holder.IsMine || !Holder.IsTurn) return;

        OnClickCard?.Invoke(this, Holder);
        OnPlayCard?.Invoke(this);
    }
    public void ResetRotate()
    {
        _isFlipped = false;
        transform.localRotation = Quaternion.Euler(90f, 0f, _initRotation.z);
        Connections = (bool[])_cardInforSO.Connections.Clone();
    }

    #endregion
}
