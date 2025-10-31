using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Collections;
using UnityEngine.Events;


[SelectionBase]
public class Card : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public CardData CardData { get; private set; }
    public CardInforSO CardInforSO => _cardInforSO;
    [SerializeField] private CardInforSO _cardInforSO;
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private CardHolder _holder;

    [Header("Card State")]
    public CardLocation Location = CardLocation.None;

    [ReadOnly] public CardType CardType;
    [ReadOnly] public PathCardType PathCardType;
    [ReadOnly] public ActionCardType ActionCardType;
    [ReadOnly] public ToolType ToolType;
    [ReadOnly] public bool[] Connections;

    public CardHolder Holder
    {
        get => _holder;
        set
        {
            _holder = value;
        }
    }

    public event UnityAction<Card> OnPlayCard = delegate { };
    public event UnityAction<Card> OnHoverCard = delegate { };
    public event UnityAction<Card> OnExitHoverCard = delegate { };
    public event UnityAction<Card> OnDiscardCard = delegate { };

    public void SetData(CardInforSO cardInforSO, CardLocation cardLocation)
    {
        _cardInforSO = cardInforSO;
        if (cardInforSO == null) return;

        if (_meshRenderer != null)
            _meshRenderer.material = cardInforSO.material;

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

        Location = cardLocation;
        CardType = cardInforSO.CardType;
        PathCardType = cardInforSO.PathCardType;
        ActionCardType = cardInforSO.ActionCardType;
        ToolType = cardInforSO.ToolType;
        Connections = (bool[])cardInforSO.Connections.Clone();
    }

    public void SetMaterial(Material material)
    {
        if (_meshRenderer != null)
            _meshRenderer.material = material;
    }

    public void Refresh()
    {
        _cardInforSO = null;
        _meshRenderer.material = null;
        CardType = CardType.None;
        PathCardType = PathCardType.None;
        Connections = null;
    }

    public void Rotate()
    {
        if (Connections == null || Connections.Length < 4) return;

        transform.localEulerAngles = new Vector3(90f, (transform.localEulerAngles.y + 180f) % 360f, 0f);

        bool[] newCon = new bool[4];
        // For a 180° rotation swap opposite connections: 0<->2, 1<->3
        newCon[0] = Connections[2];
        newCon[1] = Connections[3];
        newCon[2] = Connections[0];
        newCon[3] = Connections[1];
        Connections = newCon;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Holder == null || !Holder.IsMine || !Holder.IsTurn) return;

        if (Location == CardLocation.PlayerHand)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // if this card did not pass the selecting process, it can not be played
                if (!Holder.IsTheSelectingCard(this))
                    return;
                OnPlayCard?.Invoke(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnDiscardCard.Invoke(this);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Holder == null || !Holder.IsMine || !Holder.IsTurn) return;

        if (Location == CardLocation.PlayerHand)
        {
            OnExitHoverCard.Invoke(this);
            if (Holder == null) return;
            Holder.UnSelectCard(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Holder == null || !Holder.IsMine || !Holder.IsTurn) return;

        if (Location == CardLocation.PlayerHand)
        {
            if (CardType == CardType.Path && Holder.IsLackedATool())
                return;
            OnHoverCard.Invoke(this);
        }
    }
}
