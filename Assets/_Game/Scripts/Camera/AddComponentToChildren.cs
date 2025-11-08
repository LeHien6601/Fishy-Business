using UnityEngine;

public class AddComponentToChildren : MonoBehaviour
{

    [ContextMenu("AddComponentToChildren")]
    private void Execute()
    {
        for (int i =0; i< transform.childCount; i++)
        {
            var c = transform.GetChild(i).gameObject;
            c.GetOrAdd<BoxCollider>();
            c.GetOrAdd<FadingObject>();
        }
    }
}