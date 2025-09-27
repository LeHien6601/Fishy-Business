using UnityEngine;

[SelectionBase]
public class Card : MonoBehaviour
{
    [SerializeField] private CardInforSO CardInforSO;
    [SerializeField] private MeshRenderer meshRenderer;
    public CardType CardType;
    public PathCardType PathCardType;   // just use if cardType is PathCard
    public bool[] Connections;

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        CardInforSO = null;
        meshRenderer.material = null;
    }

    public void SetData(CardInforSO cardInforSO = null)
    {
        if (cardInforSO != null)
        {
            CardInforSO = cardInforSO;
            CardType = CardInforSO.CardType;
            PathCardType = CardInforSO.PathCardType;
            Connections = CardInforSO.Connections;
        }
    }
    public void ApplyRotation()
    {
        if (Connections == null || Connections.Length != 4) return;

        int rot = Mathf.RoundToInt(transform.eulerAngles.y / 90f) % 4;
        if (rot == 0) return;

        bool[] newCon = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            newCon[(i + rot) % 4] = Connections[i];
        }
        Connections = newCon;
    }

    [ContextMenu("Set Material")]
    public void SetMaterial()
    {
        if (CardInforSO != null && meshRenderer != null)
        {
            meshRenderer.material = CardInforSO.material;

            CardType = CardInforSO.CardType;
            PathCardType = CardInforSO.PathCardType;
            Connections = CardInforSO.Connections;
        }
    }

}
