using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct GoalTracer : INetworkSerializable
{
    public Vector2Int GoalSlot;
    public List<Vector2Int> Path;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Serialize GoalSlot
        serializer.SerializeValue(ref GoalSlot);

        // Serialize List<Vector2Int> Path
        if (serializer.IsWriter)
        {
            int count = Path != null ? Path.Count : 0;
            serializer.SerializeValue(ref count);
            for (int i = 0; i < count; i++)
            {
                var point = Path[i];
                serializer.SerializeValue(ref point);
            }
        }
        else // IsReader
        {
            int count = 0;
            serializer.SerializeValue(ref count);
            Path = new List<Vector2Int>(count);
            for (int i = 0; i < count; i++)
            {
                Vector2Int point = default;
                serializer.SerializeValue(ref point);
                Path.Add(point);
            }
        }
    }
}
