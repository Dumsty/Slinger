using UnityEngine;
using Unity.Netcode;

public class CameraMouseCon : NetworkBehaviour
{   
    public static float mouseSensitivity = 1.0f;
    public static float fieldOfView = 60f;
    [SerializeField] private float slideHeight = -0.3f;
    private PlayerMovement playerMovement;
    private Camera cam;

    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Vector3 baseLocalPos;

    void Start()
    {
        baseLocalPos = transform.localPosition;
        playerMovement = GetComponentInParent<PlayerMovement>();
        cam = GetComponent<Camera>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        ApplyFOV();
    }

    void Update()
    {
        if (!IsOwner) return;
        if (PauseMenu.paused) return;
        if (DeckStation.editingDeck) return;

        yaw += mouseSensitivity * Input.GetAxis("Mouse X");
        pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

        bool crouching = playerMovement != null && playerMovement.Crouching;
        Vector3 target = baseLocalPos + (crouching ? Vector3.up * slideHeight : Vector3.zero);
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, 10f * Time.deltaTime);
    }

    public void ApplyFOV()
    {
        if (cam != null) cam.fieldOfView = fieldOfView;
    }
}