using UnityEngine;
using Unity.Netcode;

/// <summary>
/// First-person player movement: walking, sliding, crouching, jumping
/// (with configurable extra air jumps), and slope handling. Horizontal
/// movement is driven through Rigidbody velocity rather than MovePosition
/// so wall collisions resolve smoothly instead of jittering.
/// </summary>
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float playerSpeed;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float slideThreshold = 4f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float slideDrag = 0.5f;
    [SerializeField] private float steerStrength = 2f;
    [SerializeField] private float slopeAccel = 10f;
    [SerializeField] private float maxSlideSpeed = 12f;
    [SerializeField] private float accelTime = 0.15f;
    [SerializeField] private float airAccelTime = 0.8f;
    [SerializeField] private float groundedGrace = 0.15f;

    [Header("Slopes")]
    [Tooltip("Slopes steeper than this are treated as walls and can't be climbed.")]
    [SerializeField] private float maxSlopeAngle = 50f;

    [Tooltip("Largest upward speed physics may hand us while not jumping. " +
             "Stops a push into a steep face from compounding into a launch.")]
    [SerializeField] private float maxClimbSpeed = 1.5f;

    [Tooltip("Grace period after a jump where the climb clamp is skipped.")]
    [SerializeField] private float jumpClampGrace = 0.2f;

    public float airJumpForce = 5f;
    public int ExtraJumps;

    // Buff bonuses applied/removed by cards (MoveSpeedCard, AirControlCard).
    public float SpeedBonus;
    public float AirControlBonus;

    public bool Crouching { get; private set; }
    public bool IsGrounded => grounded;
    public bool IsSliding => sliding;

    private Rigidbody rb;
    private bool grounded;
    private float groundedTimer;
    private bool sliding;
    private Vector3 lastMove;
    private Vector3 currentVelocity;
    private int airJumpsUsed;

    // Steep-surface tracking, written by the collision callbacks.
    private Vector3 steepNormal;
    private bool touchingSteep;
    private float lastJumpTime = -10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        if (PauseMenu.paused)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        UpdateGroundedState();
        bool effectivelyGrounded = groundedTimer > 0;
        bool wantsCrouch = !UITyping.IsTyping && Input.GetKey(KeyCode.C) && effectivelyGrounded;
        Crouching = wantsCrouch;

        Vector3 inputDir = GetCameraRelativeInput();

        if (wantsCrouch && (sliding || currentVelocity.magnitude >= slideThreshold))
        {
            HandleSliding(inputDir);
            return;
        }

        if (sliding)
        {
            sliding = false;
            currentVelocity = Vector3.ClampMagnitude(currentVelocity, playerSpeed);
        }

        HandleGroundedMovement(inputDir, effectivelyGrounded, wantsCrouch);
    }

    void Update()
    {
        if (!IsOwner) return;
        if (PauseMenu.paused) return;
        if (UITyping.IsTyping) return;

        if (Input.GetButtonDown("Jump")) HandleJump();
    }

    // groundedGrace gives a short window after leaving the ground where
    // jumps/landing logic still treat the player as grounded, smoothing
    // over single-frame collision gaps (e.g. running over small bumps).
    private void UpdateGroundedState()
    {
        if (grounded) { groundedTimer = groundedGrace; airJumpsUsed = 0; }
        else groundedTimer -= Time.fixedDeltaTime;
    }

    private Vector3 GetCameraRelativeInput()
    {
        if (UITyping.IsTyping) return Vector3.zero;

        Vector3 dir = Camera.main.transform.TransformDirection(
            new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")));
        dir.y = 0;
        return dir;
    }

    // Removes the part of a movement direction that pushes into a too-steep
    // face. Without this the solver resolves the overlap by shoving us
    // upward, and that upward speed compounds every physics step.
    private Vector3 StripIntoSteep(Vector3 dir)
    {
        if (!touchingSteep) return dir;

        Vector3 n = steepNormal;
        n.y = 0f;
        if (n.sqrMagnitude < 1e-6f) return dir;

        n.Normalize();
        float into = Vector3.Dot(dir, n);
        return into < 0f ? dir - n * into : dir;
    }

    // Keeps whatever vertical velocity physics produced, except when it's
    // an unreasonably fast climb that we didn't ask for by jumping.
    private float ClampedVerticalVelocity()
    {
        float vy = rb.linearVelocity.y;

        if (Time.time - lastJumpTime > jumpClampGrace && vy > maxClimbSpeed)
            vy = maxClimbSpeed;

        return vy;
    }

    private void HandleSliding(Vector3 steerInput)
    {
        sliding = true;
        lastMove = Vector3.Lerp(lastMove, Vector3.zero, slideDrag * Time.fixedDeltaTime);

        // Accelerate down slopes proportional to their steepness.
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
            lastMove += slopeDir * slopeAccel * (slopeAngle / 90f) * Time.fixedDeltaTime;
        }

        float speed = lastMove.magnitude;
        lastMove += steerInput * steerStrength;
        lastMove = Vector3.ClampMagnitude(lastMove, Mathf.Min(speed, maxSlideSpeed));
        lastMove = StripIntoSteep(lastMove);

        rb.linearVelocity = new Vector3(lastMove.x, ClampedVerticalVelocity(), lastMove.z);
        currentVelocity = new Vector3(lastMove.x, 0, lastMove.z);

        if (currentVelocity.magnitude < 0.5f) sliding = false;
    }

    private void HandleGroundedMovement(Vector3 inputDir, bool effectivelyGrounded, bool wantsCrouch)
    {
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);
        inputDir = StripIntoSteep(inputDir);

        float speedTarget = (wantsCrouch ? crouchSpeed : playerSpeed) + SpeedBonus;
        Vector3 targetVelocity = inputDir * speedTarget;

        // Air acceleration ramps slower than ground by default; AirControlBonus
        // reduces that ramp time while active.
        float ramp = effectivelyGrounded ? accelTime : Mathf.Max(0.05f, airAccelTime - AirControlBonus);
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, (playerSpeed / ramp) * Time.fixedDeltaTime);

        lastMove = currentVelocity;

        // Set via velocity (not MovePosition) so wall collisions are
        // resolved naturally by physics instead of causing jitter.
        rb.linearVelocity = new Vector3(lastMove.x, ClampedVerticalVelocity(), lastMove.z);
    }

    private void HandleJump()
    {
        if (grounded)
        {
            lastJumpTime = Time.time;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else if (airJumpsUsed < ExtraJumps)
        {
            lastJumpTime = Time.time;
            airJumpsUsed++;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * airJumpForce, ForceMode.Impulse);
        }
    }

    // Teleports via the Rigidbody (not just transform.position) and clears
    // velocity/momentum, otherwise the next physics step can silently undo
    // a transform-only teleport.
    public void Teleport(Vector3 pos)
    {
        rb.position = pos;
        rb.linearVelocity = Vector3.zero;
        currentVelocity = Vector3.zero;
        lastMove = Vector3.zero;
        transform.position = pos;
    }

    // Contacts are classified by their normal. Mostly-upward means we're
    // standing on something; anything shallower is a wall or a slope too
    // steep to walk up, which movement must not push into.
    void OnCollisionStay(Collision collision)
    {
        float slopeCos = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);

        foreach (ContactPoint contact in collision.contacts)
        {
            float ny = contact.normal.y;

            if (ny > slopeCos)
            {
                grounded = true;
            }
            else if (ny > -0.1f)
            {
                touchingSteep = true;
                steepNormal = contact.normal;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        grounded = false;
        touchingSteep = false;
    }
}