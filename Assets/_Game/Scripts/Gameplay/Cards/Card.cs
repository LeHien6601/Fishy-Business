using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Unity.Collections;

[SelectionBase]
public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public CardInforSO CardInforSO;
    [SerializeField] private MeshRenderer _meshRenderer;

    [Header("Card State")]
    public CardLocation Location = CardLocation.None;
    [HideInInspector] public CardHolder Holder;

    [ReadOnly] public CardType CardType;
    [ReadOnly] public PathCardType PathCardType;
    [ReadOnly] public bool[] Connections;

    private bool _isFlipped = false;
    private Quaternion _initRotation;
    private int _indexInHolder = -1;

    void Awake()
    {
        _initRotation = transform.localRotation;
    }

    #region Set Data
    public void SetData(CardInforSO cardInforSO)
    {
        CardInforSO = cardInforSO;
        if (cardInforSO == null) return;

        if (_meshRenderer != null)
            _meshRenderer.material = cardInforSO.material;

        _isFlipped = false;
        CardType = cardInforSO.CardType;
        PathCardType = cardInforSO.PathCardType;
        Connections = (bool[])cardInforSO.Connections.Clone();
    }
    [ContextMenu("Set Material")]
    public void SetMaterial()
    {
        if (CardInforSO != null && _meshRenderer != null)
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
        CardInforSO = null;
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
        transform.localRotation = Quaternion.Euler(90f, _isFlipped ? 180f : 0f, _initRotation.z);

        bool[] newCon = new bool[4];
        newCon[0] = Connections[2];
        newCon[1] = Connections[3];
        newCon[2] = Connections[0];
        newCon[3] = Connections[1];
        Connections = newCon;
    }

    public void SetLocation(CardLocation cardLocation)
    {
        Location = cardLocation;
    }
    #endregion

    #region Handle Hover
    // ------------------------
    // 🔹 UI Interaction Handling
    // ------------------------
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Location != CardLocation.PlayerHand) return;
        if (Holder == null) return;

        _indexInHolder = Holder.GetCardIndex(this);
        if (_indexInHolder >= 0)
            Holder.SelectCard(_indexInHolder);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Location != CardLocation.PlayerHand || Holder == null) return;

        if (_indexInHolder >= 0)
        {
            Holder.UnSelectCard(_indexInHolder);
            _indexInHolder = -1;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Location == CardLocation.PlayerHand)
        {
            Debug.Log($"🃏 Card clicked: {CardInforSO.name}");
            // TODO: implement use card, play to board, discard, etc.
        }
    }
    #endregion
}
