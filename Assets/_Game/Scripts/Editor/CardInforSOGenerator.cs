using UnityEngine;
using UnityEditor;
using System.IO;

public class CardInforSOGenerator
{
    private const string MATERIALS_PATH = "Assets/_Game/Materials/Cards";
    private const string OUTPUT_PATH = "Assets/_Game/ScriptableObjects/Cards";

    [MenuItem("Tools/Re-Generate CardInforSO from Materials")]
    public static void GenerateCardInforSOs()
    {
        if (!Directory.Exists(OUTPUT_PATH))
            Directory.CreateDirectory(OUTPUT_PATH);

        // XÓA TẤT CẢ ScriptableObject có tên chứa "CardInforSO"
        string[] existingSOPaths = Directory.GetFiles(OUTPUT_PATH, "*.asset", SearchOption.AllDirectories);
        int deletedCount = 0;
        foreach (string path in existingSOPaths)
        {
            if (Path.GetFileNameWithoutExtension(path).Contains("CardInforSO"))
            {
                AssetDatabase.DeleteAsset(path);
                deletedCount++;
            }
        }

        Debug.Log($"🗑️ Đã xóa {deletedCount} ScriptableObject cũ có tên chứa 'CardInforSO'");

        // TÌM VÀ TẠO MỚI DỰA TRÊN MATERIAL
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MATERIALS_PATH });
        int createdCount = 0;

        foreach (string guid in materialGuids)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            string soName = mat.name; // tên giống hệt material
            string soPath = Path.Combine(OUTPUT_PATH, soName + ".asset");

            // Nếu trùng tên thì bỏ qua
            if (AssetDatabase.LoadAssetAtPath<CardInforSO>(soPath) != null)
                continue;

            // Tạo mới ScriptableObject
            CardInforSO newSO = ScriptableObject.CreateInstance<CardInforSO>();
            newSO.material = mat;

            // Giá trị mặc định (bạn có thể tùy chỉnh)
            newSO.Connections = new bool[4];
            newSO.CardType = default;
            newSO.PathCardType = default;
            newSO.ActionCardType = default;
            newSO.ToolType = default;

            AssetDatabase.CreateAsset(newSO, soPath);
            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Đã tạo {createdCount} CardInforSO mới trong thư mục: {OUTPUT_PATH}");
    }
}
