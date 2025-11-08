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
    [SerializeField] private MeshRenderer _outliner;
    [SerializeField] private SpriteRenderer _symbol;
    [SerializeField] private Material _redHighlight;
    [SerializeField] private Material _yellowHighlight;
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

        if (_symbol)
        {
            _symbol.gameObject.SetActive(false);
        }
        Location = cardLocation;
        CardType = cardInforSO.CardType;
        PathCardType = cardInforSO.PathCardType;
        ActionCardType = cardInforSO.ActionCardType;
        ToolType = cardInforSO.ToolType;
        Connections = (bool[])cardInforSO.Connections.Clone();
    }
    // Call when deal or draw card to hand player
    public void SetData(CardData cardData, CardInforSO cardInforSO, CardLocation cardLocation)
    {
        CardData = cardData;
        _cardInforSO = cardInforSO;
        if (cardInforSO == null) return;

        if (_meshRenderer != null)
            _meshRenderer.material = cardInforSO.material;

        if (_symbol && cardInforSO.Symbol)
        {
            _symbol.gameObject.SetActive(true);
            _symbol.sprite = cardInforSO.Symbol;
        }
        else
        {
            _symbol.gameObject.SetActive(false);
        }
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
        
        if (_symbol)
        {
            _symbol.gameObject.SetActive(false);
        }
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

    public Quaternion GetRealRotation()
    {
        if (CardType != CardType.Path || Connections == null || Connections.Length < 4) return Quaternion.Euler(new Vector3(90f, 0, 0f));
        for (int i = 0; i < 4; i++)
        {
            if (Connections[i] != CardInforSO.Connections[i])
                return Quaternion.Euler(new Vector3(90f, 180f, 0f));
        }
        return Quaternion.Euler(new Vector3(90f, 0, 0f));
    }


    public void Highlight(bool ok)
    {
        if (ok)
        {
            _outliner.gameObject.SetActive(true);
            _outliner.material = _yellowHighlight;
        }
        else
        {
            _outliner.gameObject.SetActive(true);
            _outliner.material = _redHighlight;
        }
    }

    public void OffHighlight()
    {
        _outliner.gameObject.SetActive(false);
    }

    // --- these pointer handlers downhere only send the events, delegate actual logic to BoardManager ---

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
            if(_symbol)
            {
                _symbol.gameObject.SetActive(false);
            }
        }
        OffHighlight(); // turn of if any
    }
    

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Holder == null || !Holder.IsMine || !Holder.IsTurn) return;

        if (Location == CardLocation.PlayerHand)
        {
            OnExitHoverCard.Invoke(this);
            Holder.UnSelectCard(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Holder == null || !Holder.IsMine || !Holder.IsTurn) return;

        if (Location == CardLocation.PlayerHand)
        {
            OnHoverCard.Invoke(this);
        }
    }

    public void TriggerDiscardCard()
    {
        OnDiscardCard.Invoke(this);
    }
}
