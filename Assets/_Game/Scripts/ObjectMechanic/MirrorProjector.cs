using UnityEngine;

public class MirrorProjector : MonoBehaviour
{
    public Transform PlayerCam;
    
    // NOTE: Drag your Camera into this slot instead of a Transform!
    public Camera MirrorCamera; 
    
    public float maxVisibleDistance = 20f;
    
    private float _maxDistanceSq;

    private void Awake()
    {
        // Pre-calculate the square of the distance to avoid Mathf.Sqrt in Update
        _maxDistanceSq = maxVisibleDistance * maxVisibleDistance;
        
        if (PlayerCam == null && Camera.main != null)
        {
            PlayerCam = Camera.main.transform;
        }
    }

    // Automatically called when the mesh enters ANY camera view
    private void OnBecameVisible()
    {
        enabled = true; // Turn Update ON so we can track distance
    }

    // Automatically called when the mesh leaves ALL camera views
    private void OnBecameInvisible()
    {
        enabled = false; // Completely stop the Update loop
        if (MirrorCamera != null) MirrorCamera.enabled = false; // Stop rendering
    }

    void Update()
    {
        if (PlayerCam == null || MirrorCamera == null) return;

        // 1. Fast Distance Check (No square roots!)
        Vector3 offsetToPlayer = PlayerCam.position - transform.position;
        
        if (offsetToPlayer.sqrMagnitude > _maxDistanceSq)
        {
            // On screen, but too far: Turn off camera, skip the math
            if (MirrorCamera.enabled) MirrorCamera.enabled = false;
            return;
        }

        // 2. On screen and close enough: ensure camera is on
        if (!MirrorCamera.enabled) MirrorCamera.enabled = true;

        // 3. Optimized Math (Flatten the Y axis without extra Vector3 allocations)
        Vector3 side1 = new Vector3(offsetToPlayer.x, 0, offsetToPlayer.z);
        
        float angle = Vector3.SignedAngle(side1, transform.forward, Vector3.up);
        MirrorCamera.transform.localEulerAngles = new Vector3(0, angle * 0.2f, 0);
    }

    // Automatically updates the squared distance if you tweak the value in the Editor
    private void OnValidate()
    {
        _maxDistanceSq = maxVisibleDistance * maxVisibleDistance;
    }
}