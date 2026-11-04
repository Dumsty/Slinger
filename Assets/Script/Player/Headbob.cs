using UnityEngine;

/// <summary>
/// Subtle first-person head bob plus the crouch camera dip.
///
/// IMPORTANT: put this on a dedicated pivot object that sits BETWEEN the
/// player and the camera, not on the camera itself:
///
///     Player
///     └── Camera Pivot   <- this script
///         └── FPS Camera <- CameraMouseCon, local position zeroed
///
/// The pivot is written absolutely each frame, so nothing accumulates even
/// when mouse look stops updating (paused, shop open). The bob is computed
/// in the VIEW's horizontal space, so sway stays side-to-side relative to
/// where the player is looking rather than to the player's own axes.
/// </summary>
public class HeadBob : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-find. Needed to keep sway aligned to the view.")]
    public Transform cameraTransform;
    public PlayerMovement movement;
    public Rigidbody body;

    [Header("Bob shape")]
    [Tooltip("Up/down travel in metres at full speed. Keep this small.")]
    public float verticalAmount = 0.045f;

    [Tooltip("Side-to-side travel, as a fraction of the vertical amount.")]
    [Range(0f, 1f)] public float horizontalRatio = 0.5f;

    [Tooltip("Steps per second at full speed.")]
    public float frequency = 9f;

    [Tooltip("Degrees of camera roll at the extremes of the sway. 0 disables it.")]
    public float rollAmount = 0.4f;

    [Header("Speed response")]
    [Tooltip("Speed at which the bob reaches full strength.")]
    public float referenceSpeed = 6f;

    [Tooltip("Below this speed the bob fades out entirely.")]
    public float moveThreshold = 0.4f;

    [Header("Crouch")]
    [Tooltip("How far the camera lowers while crouching, in metres.")]
    public float crouchDrop = 0.4f;

    [Tooltip("How quickly the camera drops and rises. Higher is snappier.")]
    public float crouchSpeed = 12f;

    [Tooltip("Bob strength multiplier while crouching.")]
    public float crouchAmountMultiplier = 0.5f;

    [Tooltip("Bob speed multiplier while crouching.")]
    public float crouchFrequencyMultiplier = 0.6f;

    [Header("Smoothing")]
    [Tooltip("How quickly the bob eases in and out. Higher is snappier.")]
    public float smoothing = 10f;

    private Vector3 basePosition;
    private Vector3 currentOffset;      // x = view-right, y = world-up
    private float currentRoll;
    private float currentDrop;
    private float phase;

    void Awake()
    {
        basePosition = transform.localPosition;

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
        if (movement == null) movement = GetComponentInParent<PlayerMovement>();
        if (body == null) body = GetComponentInParent<Rigidbody>();
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        // --- crouch dip: independent of the bob, so it still applies while
        // sliding, paused or in the shop ---
        bool crouching = movement != null && (movement.Crouching || movement.IsSliding);
        float targetDrop = crouching ? crouchDrop : 0f;
        currentDrop = Mathf.Lerp(currentDrop, targetDrop, 1f - Mathf.Exp(-crouchSpeed * dt));

        // --- bob ---
        Vector3 targetOffset = Vector3.zero;
        float targetRoll = 0f;

        if (ShouldBob(out float speedFactor))
        {
            float amountMul = movement.Crouching ? crouchAmountMultiplier : 1f;
            float freqMul   = movement.Crouching ? crouchFrequencyMultiplier : 1f;

            phase += dt * frequency * freqMul * speedFactor;

            float amp = verticalAmount * amountMul * speedFactor;

            // Horizontal runs at half the vertical frequency, which traces a
            // figure-eight and reads as weight shifting between feet.
            targetOffset = new Vector3(
                Mathf.Cos(phase * 0.5f) * amp * horizontalRatio,
                Mathf.Sin(phase) * amp,
                0f);

            targetRoll = -Mathf.Cos(phase * 0.5f) * rollAmount * amountMul * speedFactor;
        }
        else
        {
            phase = Mathf.Repeat(phase, Mathf.PI * 4f);
        }

        float t = 1f - Mathf.Exp(-smoothing * dt);   // framerate-independent
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, t);
        currentRoll   = Mathf.Lerp(currentRoll, targetRoll, t);

        ApplyToPivot();
    }

    // The camera owns yaw, not the player body, so the pivot's own axes don't
    // follow the view. Build a flattened view basis in world space, then
    // convert into the pivot's parent space before assigning.
    void ApplyToPivot()
    {
        Vector3 viewForward = Vector3.forward;
        if (cameraTransform != null)
        {
            viewForward = cameraTransform.forward;
            viewForward.y = 0f;
            // Looking straight up or down leaves nothing to flatten.
            if (viewForward.sqrMagnitude < 1e-6f)
            {
                viewForward = cameraTransform.up;
                viewForward.y = 0f;
            }
            if (viewForward.sqrMagnitude < 1e-6f) viewForward = Vector3.forward;
            viewForward.Normalize();
        }

        Vector3 viewRight = Vector3.Cross(Vector3.up, viewForward);

        Vector3 worldOffset = viewRight * currentOffset.x + Vector3.up * currentOffset.y;
        Vector3 worldDown   = Vector3.down * currentDrop;

        Transform p = transform.parent;
        Vector3 localOffset  = p != null ? p.InverseTransformDirection(worldOffset + worldDown)
                                         : worldOffset + worldDown;
        Vector3 localForward = p != null ? p.InverseTransformDirection(viewForward)
                                         : viewForward;

        // Assigned, never multiplied in - nothing can accumulate.
        transform.localPosition = basePosition + localOffset;
        // Roll about the view's forward axis, so it reads as a tilt at any heading.
        transform.localRotation = Quaternion.AngleAxis(currentRoll, localForward);
    }

    bool ShouldBob(out float speedFactor)
    {
        speedFactor = 0f;

        if (movement == null || body == null) return false;
        if (PauseMenu.paused || DeckStation.editingDeck) return false;
        if (!movement.IsGrounded || movement.IsSliding) return false;

        Vector3 flat = body.linearVelocity;
        flat.y = 0f;
        float speed = flat.magnitude;
        if (speed < moveThreshold) return false;

        speedFactor = Mathf.Clamp01(speed / Mathf.Max(0.1f, referenceSpeed));
        return true;
    }

    /// <summary>Snaps the pivot back to neutral - call after a teleport.</summary>
    public void ResetBob()
    {
        currentOffset = Vector3.zero;
        currentRoll = 0f;
        currentDrop = 0f;
        phase = 0f;
        transform.localPosition = basePosition;
        transform.localRotation = Quaternion.identity;
    }
}