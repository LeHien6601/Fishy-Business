// #if UNITY_EDITOR
// using UnityEditor;
// #endif

using UnityEngine;

[CreateAssetMenu(fileName = "CardInforSO", menuName = "CardInforSO")]
public class CardInforSO : ScriptableObject
{
    public Material material;
    public Sprite Symbol;
    public bool[] Connections;
    public CardType CardType;
    public PathCardType PathCardType;
    public ActionCardType ActionCardType;
    public ToolType ToolType;

// #if UNITY_EDITOR
//     [ContextMenu("Find Symbol Automatically")]
//     public void FindSymbolAutomatically()
//     {
//         if (material == null)
//         {
//             Debug.LogWarning("Material chưa được gán!");
//             return;
//         }

//         string matName = material.name; // ví dụ: Card_Deadend_ES
//         string symbolName = matName.Replace("Card", "Symbol");

//         string[] guids = AssetDatabase.FindAssets(symbolName + " t:Sprite", new[] { "Assets/_Game/Textures/Symbols" });
//         if (guids.Length == 0)
//         {
//             Debug.LogWarning($"Không tìm thấy sprite có tên {symbolName}");
//             return;
//         }

//         string path = AssetDatabase.GUIDToAssetPath(guids[0]);
//         Symbol = AssetDatabase.LoadAssetAtPath<Sprite>(path);
//         Debug.Log($"Đã gán symbol {symbolName} cho {name}");
//         EditorUtility.SetDirty(this);
//     }
// #endif
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
    Binoculars,
    Shield,
    SwapGoal,
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