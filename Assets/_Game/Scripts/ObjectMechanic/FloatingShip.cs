using UnityEngine;

public class FloatingShip : MonoBehaviour
{
    [Header("Bobbing")]
    [Tooltip("How high the ship bobs up and down")]
    public float bobAmplitude = 0.3f;
    [Tooltip("How fast the ship bobs up and down")]
    public float bobFrequency = 0.6f;

    [Header("Rocking (Pitch & Roll)")]
    [Tooltip("Max fore/aft tilt angle in degrees")]
    public float pitchAmplitude = 3f;
    [Tooltip("How fast the ship pitches")]
    public float pitchFrequency = 0.45f;

    [Tooltip("Max side-to-side tilt angle in degrees")]
    public float rollAmplitude = 5f;
    [Tooltip("How fast the ship rolls")]
    public float rollFrequency = 0.35f;

    [Header("Drift (Horizontal Sway)")]
    [Tooltip("How far the ship drifts horizontally")]
    public float driftAmplitude = 0.05f;
    [Tooltip("How fast the ship drifts")]
    public float driftFrequency = 0.25f;

    [Header("Phase Offsets (randomised on Awake)")]
    [Tooltip("Enable to randomise phase offsets so multiple ships look different")]
    public bool randomisePhases = true;

    // Cached origin so the float is always relative to where the ship started
    private Vector3 _originPosition;
    private Quaternion _originRotation;

    // Per-axis phase offsets
    private float _bobPhase;
    private float _pitchPhase;
    private float _rollPhase;
    private float _driftXPhase;
    private float _driftZPhase;

    private void Awake()
    {
        _originPosition = transform.localPosition;
        _originRotation = transform.localRotation;

        if (randomisePhases)
        {
            _bobPhase    = Random.Range(0f, Mathf.PI * 2f);
            _pitchPhase  = Random.Range(0f, Mathf.PI * 2f);
            _rollPhase   = Random.Range(0f, Mathf.PI * 2f);
            _driftXPhase = Random.Range(0f, Mathf.PI * 2f);
            _driftZPhase = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void Update()
    {
        float t = Time.time;

        // --- Position ---
        float bobY   = Mathf.Sin(t * bobFrequency   * Mathf.PI * 2f + _bobPhase)    * bobAmplitude;
        float driftX = Mathf.Sin(t * driftFrequency * Mathf.PI * 2f + _driftXPhase) * driftAmplitude;
        // Use a slightly different frequency on Z so drift isn't perfectly linear
        float driftZ = Mathf.Sin(t * driftFrequency * Mathf.PI * 1.3f + _driftZPhase) * driftAmplitude * 0.6f;

        transform.localPosition = _originPosition + new Vector3(driftX, bobY, driftZ);

        // --- Rotation ---
        float pitch = Mathf.Sin(t * pitchFrequency * Mathf.PI * 2f + _pitchPhase) * pitchAmplitude;
        float roll  = Mathf.Sin(t * rollFrequency  * Mathf.PI * 2f + _rollPhase)  * rollAmplitude;

        // Apply on top of the ship's original rotation so any authored tilt is preserved
        transform.localRotation = _originRotation * Quaternion.Euler(pitch, 0f, roll);
    }

#if UNITY_EDITOR
    // Reset() is called when the component is first added in the editor —
    // useful so the inspector shows sensible defaults immediately.
    private void Reset()
    {
        bobAmplitude   = 0.3f;   bobFrequency   = 0.6f;
        pitchAmplitude = 3f;     pitchFrequency = 0.45f;
        rollAmplitude  = 5f;     rollFrequency  = 0.35f;
        driftAmplitude = 0.05f;  driftFrequency = 0.25f;
        randomisePhases = true;
    }
#endif
}