using UnityEngine;

public class HandFollow : MonoBehaviour
{
    public Transform cam;
    public Vector3 offset = new Vector3(0, -0.3f, 1.5f);
    public float smoothSpeed = 5f;

    void Update()
    {
        Vector3 targetPos = cam.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, cam.rotation, smoothSpeed * Time.deltaTime);
    }
}