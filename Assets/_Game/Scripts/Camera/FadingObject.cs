using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// detect using raycast, so we need a collider
/// attach this component to any object that should fade when it comes between the camera and the target
/// </summary>
[RequireComponent(typeof(Collider))]
public class FadingObject : MonoBehaviour, IEquatable<FadingObject>
{
    private List<Renderer> _renderers = new();
    private Vector3 _pos;
    [HideInInspector] public List<Material> Materials = new();
    [HideInInspector] public float InitialAlpha;

    private void Awake()
    {
        _pos = transform.position;

        if (_renderers.Count == 0)
        {
            _renderers.AddRange(GetComponentsInChildren<Renderer>());
        }
        foreach (Renderer renderer in _renderers)
        {
            Materials.AddRange(renderer.materials);
        }

        InitialAlpha = Materials[0].color.a;
    }

    public bool Equals(FadingObject other)
    {
        return _pos.Equals(other._pos);
    }

    public override int GetHashCode()
    {
        return _pos.GetHashCode();
    }
}
