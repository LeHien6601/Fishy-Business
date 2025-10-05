using UnityEngine;

[SelectionBase]
public class Card : MonoBehaviour
{
    [SerializeField] private CardInforSO CardInforSO;
    [SerializeField] private MeshRenderer meshRenderer;
    public CardType CardType;
    public PathCardType PathCardType;   // just use if cardType is PathCard
    public bool[] Connections;
    private bool isFlipped = false; // false = normal, true = rotated 180°
    private Quaternion _initRotation;
    void Awake()
    {
        _initRotation = transform.localRotation;
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        CardInforSO = null;
        meshRenderer.material = null;

        CardType = CardType.None;
        PathCardType = PathCardType.None;
        Connections = null;
        isFlipped = false;
        transform.localRotation = _initRotation;
    }

    public void SetData(CardInforSO cardInforSO = null)
    {
        if (cardInforSO != null)
        {
            CardInforSO = cardInforSO;
            if (meshRenderer != null)
            {
                meshRenderer.material = CardInforSO.material;
            }
            CardType = cardInforSO.CardType;
            PathCardType = CardInforSO.PathCardType;
            Connections = (bool[])cardInforSO.Connections.Clone(); // clone để giữ asset gốc
            isFlipped = false;
            transform.localRotation = _initRotation;
        }
    }

    /// <summary>
    /// Rotate 180° around Y, swap N<->S and E<->W in Connections.
    /// </summary>
    [ContextMenu("Rotate")]
    public void Rotate()
    {
        if (Connections == null || Connections.Length < 4) return;

        isFlipped = !isFlipped;
        transform.localRotation = Quaternion.Euler(90f, isFlipped ? 180f : 0f, _initRotation.z);

        // N,E,S,W = 0,1,2,3 -> swap 0<->2, 1<->3
        bool[] newCon = new bool[4];
        newCon[0] = Connections[2]; // N = S
        newCon[1] = Connections[3]; // E = W
        newCon[2] = Connections[0]; // S = N
        newCon[3] = Connections[1]; // W = E

        Connections = newCon;
    }

    [ContextMenu("Set Material")]
    public void SetMaterial()
    {
        if (CardInforSO != null && meshRenderer != null)
        {
            // Reset Flip
            isFlipped = false;
            transform.localRotation = Quaternion.Euler(90f, 0f, _initRotation.z);

            // Set new Material
            meshRenderer.material = CardInforSO.material;

            CardType = CardInforSO.CardType;
            PathCardType = CardInforSO.PathCardType;
            Connections = (bool[])CardInforSO.Connections.Clone();
        }
    }

}
