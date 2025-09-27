using UnityEngine;

[CreateAssetMenu(fileName = "CardInforSO", menuName = "CardInforSO")]
public class CardInforSO : ScriptableObject
{
    public Material material;
    public bool[] Connections;
    public CardType CardType;
    public PathCardType PathCardType;
}

public enum CardType { Path, Action, Goal }
public enum PathCardType
{
    PathCard,
    DeadEnd,
}
public enum Direction
{
    N = 0,
    E,
    S,
    W
}