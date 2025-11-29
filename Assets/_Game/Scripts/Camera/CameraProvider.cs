using UnityEngine;

public class CameraProvider : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    void OnEnable()
    {
        var uiCanvas = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (uiCanvas != null && uiCanvas.Length > 0)
        {
            foreach (var canvas in uiCanvas)
            {
                canvas.worldCamera = _camera;
            }
        }
    }
}
