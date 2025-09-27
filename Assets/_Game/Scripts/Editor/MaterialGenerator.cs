using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class MaterialGenerator
{
    [MenuItem("Tools/Generate Card Materials")]
    static void GenerateMaterials()
    {
        string textureFolder = "Assets/_Game/Textures/Cards";
        string materialFolder = "Assets/_Game/Materials/Cards";

        // Make sure output folder exists
        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            AssetDatabase.CreateFolder(textureFolder, "Materials");
        }

        // Get all textures in folder
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolder });

        foreach (string guid in guids)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            if (tex == null) continue;

            string matPath = Path.Combine(materialFolder, tex.name + ".mat");
            matPath = matPath.Replace("\\", "/");

            // Skip if material already exists
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                // Create new material
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }

            // Assign texture and tiling
            mat.mainTexture = tex;
            mat.mainTextureScale = new Vector2(3.52f, 1f);

            // Mark dirty to save changes
            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ Materials generated in " + materialFolder);
    }

}
