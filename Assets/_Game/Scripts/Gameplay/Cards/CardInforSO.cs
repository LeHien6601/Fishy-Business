using UnityEngine;

[CreateAssetMenu(fileName = "CardInforSO", menuName = "CardInforSO")]
public class CardInforSO : ScriptableObject
{
    public Material material;
    public bool[] Connections;
    public CardType CardType;
    public PathCardType PathCardType;
    public ActionCardType ActionCardType;
    public ToolType ToolType;
}

public enum CardType { None, Path, Action, Goal }
public enum PathCardType
{
    None,
    PathCard,
    DeadEnd,
}
public enum ActionCardType
{
    None,
    BrokenTool,
    FixTool,
    Bomb,
    CheckGold,
}
public enum ToolType
{
    None,
    Cart,
    Hat,
    Shovel,
    CartHat,
    CartShovel,
    HatShovel,

}
public enum Direction
{
    N = 0,
    E,
    S,
    W
}

public enum CardLocation
{
    None,
    Deck,
    PlayerHand,
    OnBoard,
    Discarded,
    Hidden,
}