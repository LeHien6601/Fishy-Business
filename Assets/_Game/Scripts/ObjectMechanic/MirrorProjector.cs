using UnityEngine;

public class MirrorProjector : MonoBehaviour
{
    public Transform PlayerCam, MirrorCam;

    private void OnEnable()
    {
        PlayerCam = Camera.main.transform;
    }

    void Update()
    {
        Vector3 posY = new(transform.position.x, PlayerCam.transform.position.y, transform.position.z);
        Vector3 side1 = PlayerCam.transform.position - posY;
        Vector3 side2 = transform.forward;
        float angle = Vector3.SignedAngle(side1, side2, Vector3.up);

        MirrorCam.localEulerAngles = new Vector3(0, angle * 0.2f, 0);
    }
}