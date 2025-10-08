using UnityEngine;

[CreateAssetMenu(fileName = "CardInforSO", menuName = "CardInforSO")]
public class CardInforSO : ScriptableObject
{
    public Material material;
    public bool[] Connections;
    public CardType CardType;
    public PathCardType PathCardType;
}

public enum CardType { None, Path, Action, Goal }
public enum PathCardType
{
    None,
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