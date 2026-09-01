using UnityEngine;
using Unity.Netcode;

public class CameraMouseCon : NetworkBehaviour
{   
    public static float mouseSensitivity = 1.0f;
    [SerializeField] private float slideHeight = -0.3f;

    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Vector3 baseLocalPos;

    void Start()
    {
        baseLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (PauseMenu.paused) return;
        
        yaw += mouseSensitivity * Input.GetAxis("Mouse X");
        pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

        Vector3 target = baseLocalPos + (Input.GetKey(KeyCode.C) ? Vector3.up * slideHeight : Vector3.zero);
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, 10f * Time.deltaTime);
    }
}