using System;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class FindCamera : MonoBehaviour
{
    public string cameraName;

    private Canvas _canvas;
    public Canvas Canvas { get { if (_canvas == null) _canvas = GetComponent<Canvas>(); return _canvas; } }

    void OnEnable()
    {
        if(!Canvas.worldCamera)
        {
            // Canvas.worldCamera = Camera.allCameras.Find(camera => camera.name == cameraName);
            Canvas.worldCamera = Array.Find(Camera.allCameras, camera => camera.name == cameraName);
        }
    }
}