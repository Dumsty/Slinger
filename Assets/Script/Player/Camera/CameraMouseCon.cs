using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles first-person camera look (mouse yaw/pitch) and applies the
/// crouch height offset. mouseSensitivity/fieldOfView are static so the
/// settings menu (in any scene) can adjust them and have them apply here
/// immediately, without needing a direct reference to this instance.
/// </summary>
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

        // Cached once rather than using Camera.main, so this always
        // controls this specific player's own camera - Camera.main could
        // resolve to any tagged "MainCamera" in a multiplayer scene.
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

        // Smoothly lowers the camera while crouching/sliding.
        bool crouching = playerMovement != null && playerMovement.Crouching;
        Vector3 target = baseLocalPos + (crouching ? Vector3.up * slideHeight : Vector3.zero);
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, 10f * Time.deltaTime);
    }

    // Called on spawn, and again by the settings menu whenever fieldOfView
    // changes mid-game so the change applies immediately.
    public void ApplyFOV()
    {
        if (cam != null) cam.fieldOfView = fieldOfView;
    }
}