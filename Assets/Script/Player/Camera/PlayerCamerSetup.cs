
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Enables the camera for the owning player and keeps
/// the Wizard facing the camera's horizontal direction.
/// </summary>
public class PlayerCameraSetup : NetworkBehaviour
{
    public Camera playerCamera;
    public AudioListener audioListener;

    [Header("Wizard")]
    public Transform wizardVisual;
    public float wizardYawOffset = 0f;

    [Header("Camera Height")]
    public Transform cameraPivot;
    public float cameraLowerAmount = 0.15f;

    private Vector3 originalCameraPosition;
    private Transform wizardRoot;

    // Only the owner can write its facing direction; all clients can read it.
    private readonly NetworkVariable<float> syncedWizardYaw =
        new NetworkVariable<float>(0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    private float displayedWizardYaw;

    private void Awake()
    {
        if (cameraPivot != null)
            originalCameraPosition = cameraPivot.localPosition;
    }

    private Transform GetWizardRoot()
    {
        if (wizardVisual == null)
            return transform.Find("Slinger");

        Transform root = wizardVisual;

        // Find the full model root, not an internal bone or mesh.
        while (root.parent != null && root.parent != transform)
            root = root.parent;

        return root;
    }

    public override void OnNetworkSpawn()
    {
        if (playerCamera != null)
            playerCamera.enabled = IsOwner;

        if (audioListener != null)
            audioListener.enabled = IsOwner;

        wizardRoot = GetWizardRoot();

        if (!IsOwner)
            return;

        if (cameraPivot != null)
        {
            cameraPivot.localPosition =
                originalCameraPosition +
                Vector3.down * cameraLowerAmount;
        }

        if (wizardRoot == null)
        {
            Debug.LogError("Wizard model not found!");
            return;
        }

        // Hide the complete Wizard for the owning client.
        // This cannot be overridden by renderer.enabled changes.
        Renderer[] renderers =
            wizardRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
            r.forceRenderingOff = true;

        Debug.Log(
            $"Hidden {renderers.Length} Wizard renderers for owner."
        );
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && wizardRoot != null)
        {
            foreach (Renderer r in
                wizardRoot.GetComponentsInChildren<Renderer>(true))
            {
                r.forceRenderingOff = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (!IsSpawned || wizardRoot == null)
            return;

        if (IsOwner && playerCamera != null)
        {
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.001f)
            {
                // Send yaw relative to the player root, leaving the
                // camera and the existing NetworkTransform untouched.
                Vector3 localForward =
                    transform.InverseTransformDirection(forward.normalized);
                float yaw = Mathf.Atan2(localForward.x, localForward.z) *
                    Mathf.Rad2Deg;

                if (Mathf.Abs(Mathf.DeltaAngle(syncedWizardYaw.Value, yaw)) > 0.2f)
                    syncedWizardYaw.Value = yaw;

                displayedWizardYaw = yaw;
            }
        }
        else
        {
            // Smooth the direction received from the owning player.
            displayedWizardYaw = Mathf.LerpAngle(
                displayedWizardYaw, syncedWizardYaw.Value,
                1f - Mathf.Exp(-15f * Time.deltaTime));
        }

        wizardRoot.localRotation =
            Quaternion.Euler(0f, displayedWizardYaw + wizardYawOffset, 0f);
    }
}
