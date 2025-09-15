using UnityEngine;

public class HouseGenerator : MonoBehaviour
{
    [Header("Prefabs (tạm thời dùng chung floorPrefab cho cả tường, cửa, cửa sổ)")]
    public GameObject floorPrefab;
    public GameObject doorPrefab;
    public GameObject windowPrefab;
    public GameObject wallPrefab;

    [Header("Floor Settings")]
    public int floors = 2;              // số tầng
    public int floorTilesPerSide = 10;  // số miếng prefab cho mỗi cạnh tầng
    public float prefabSize = 4f;       // kích thước thật của 1 prefab nền (vd: 4x4)

    [Header("Wall Settings")]
    public float wallPrefabWidth = 4f;   // bề ngang 1 tấm tường
    public float wallPrefabHeight = 4f;  // chiều cao 1 tấm tường
    public int wallHeightInPrefabs = 4;  // số lượng prefab chồng theo chiều cao
    public float wallYOffset = 0f;       // offset root tường so với sàn

    public Quaternion flatRotation = Quaternion.Euler(0f, 0f, 0f); // nền nằm ngang

    // void Start()
    // {
    //     GenerateHouse();
    // }

    [ContextMenu("Generate House")]
    void GenerateHouse()
    {
        if (floorPrefab == null)
        {
            Debug.LogError("Chưa gán floorPrefab!");
            return;
        }

        // Xóa cũ
//         for (int i = transform.childCount - 1; i >= 0; i--)
//         {
// #if UNITY_EDITOR
//             DestroyImmediate(transform.GetChild(i).gameObject);
// #else
//             Destroy(transform.GetChild(i).gameObject);
// #endif
//         }

        float floorSize = floorTilesPerSide * prefabSize;
        float halfSize = floorSize * 0.5f;

        Vector3 origin = transform.position; // center

        for (int f = 0; f < floors; f++)
        {
            // Tạo parent cho từng tầng
            GameObject floorGO = new GameObject("Floor_" + f);
            floorGO.transform.SetParent(transform);
            floorGO.transform.localPosition = Vector3.zero;

            GameObject floorTilesGO = new GameObject("Tiles");
            floorTilesGO.transform.SetParent(floorGO.transform);

            GameObject wallsGO = new GameObject("Walls");
            wallsGO.transform.SetParent(floorGO.transform);

            // sub-parents walls
            GameObject frontParent = new GameObject("Front"); frontParent.transform.SetParent(wallsGO.transform);
            GameObject backParent = new GameObject("Back"); backParent.transform.SetParent(wallsGO.transform);
            GameObject leftParent = new GameObject("Left"); leftParent.transform.SetParent(wallsGO.transform);
            GameObject rightParent = new GameObject("Right"); rightParent.transform.SetParent(wallsGO.transform);

            float baseY = f * wallHeightInPrefabs * wallPrefabHeight;

            // --- NỀN TẦNG ---
            for (int x = 0; x < floorTilesPerSide; x++)
            {
                for (int z = 0; z < floorTilesPerSide; z++)
                {
                    float px = origin.x + (x * prefabSize - halfSize + prefabSize * 0.5f);
                    float pz = origin.z + (z * prefabSize - halfSize + prefabSize * 0.5f);
                    Vector3 pos = new Vector3(px, baseY, pz);
                    GameObject floor = Instantiate(floorPrefab, pos, Quaternion.identity, floorTilesGO.transform);
                    floor.transform.localRotation = flatRotation; // ép lại rotation mong muốn
                }
            }

            // --- TƯỜNG ---
            for (int h = 0; h < wallHeightInPrefabs; h++)
            {
                float wallY = baseY + h * wallPrefabHeight + wallYOffset;

                // Front
                for (int x = 0; x < floorTilesPerSide; x++)
                {
                    GameObject prefab = (f == 0 && h == 0 && x == floorTilesPerSide / 2)
                        ? (doorPrefab ?? wallPrefab)
                        : (wallPrefab ?? floorPrefab);

                    float px = origin.x + (x * prefabSize - halfSize + prefabSize * 0.5f);
                    float pz = origin.z + halfSize;
                    Vector3 pos = new Vector3(px, wallY, pz);
                    Quaternion rot = Quaternion.Euler(0f, 180f, 0f);
                    Instantiate(prefab, pos, rot, frontParent.transform);
                }

                // Back
                for (int x = 0; x < floorTilesPerSide; x++)
                {
                    GameObject prefab = windowPrefab ?? wallPrefab ?? floorPrefab;
                    float px = origin.x + (x * prefabSize - halfSize + prefabSize * 0.5f);
                    float pz = origin.z - halfSize;
                    Vector3 pos = new Vector3(px, wallY, pz);
                    Instantiate(prefab, pos, Quaternion.identity, backParent.transform);
                }

                // Left
                for (int z = 0; z < floorTilesPerSide; z++)
                {
                    GameObject prefab = windowPrefab ?? wallPrefab ?? floorPrefab;
                    float pz = origin.z + (z * prefabSize - halfSize + prefabSize * 0.5f);
                    float px = origin.x - halfSize;
                    Vector3 pos = new Vector3(px, wallY, pz);
                    Instantiate(prefab, pos, Quaternion.Euler(0f, -90f, 0f), leftParent.transform);
                }

                // Right
                for (int z = 0; z < floorTilesPerSide; z++)
                {
                    GameObject prefab = windowPrefab ?? wallPrefab ?? floorPrefab;
                    float pz = origin.z + (z * prefabSize - halfSize + prefabSize * 0.5f);
                    float px = origin.x + halfSize;
                    Vector3 pos = new Vector3(px, wallY, pz);
                    Instantiate(prefab, pos, Quaternion.Euler(0f, 90f, 0f), rightParent.transform);
                }
            }
        }
    }
}
