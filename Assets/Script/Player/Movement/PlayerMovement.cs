using UnityEngine;
using Unity.Netcode;

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

    public float airJumpForce = 5f;
    public int ExtraJumps;
    public float SpeedBonus;
    public float AirControlBonus;

    public bool Crouching { get; private set; }

    private Rigidbody rb;
    private bool grounded;
    private float groundedTimer;
    private bool sliding;
    private Vector3 lastMove;
    private Vector3 currentVelocity;
    private int airJumpsUsed;

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
        bool wantsCrouch = Input.GetKey(KeyCode.C) && effectivelyGrounded;
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

        if (Input.GetButtonDown("Jump")) HandleJump();
    }

    private void UpdateGroundedState()
    {
        if (grounded) { groundedTimer = groundedGrace; airJumpsUsed = 0; }
        else groundedTimer -= Time.fixedDeltaTime;
    }

    private Vector3 GetCameraRelativeInput()
    {
        Vector3 dir = Camera.main.transform.TransformDirection(
            new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")));
        dir.y = 0;
        return dir;
    }

    private void HandleSliding(Vector3 steerInput)
    {
        sliding = true;
        lastMove = Vector3.Lerp(lastMove, Vector3.zero, slideDrag * Time.fixedDeltaTime);

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
            lastMove += slopeDir * slopeAccel * (slopeAngle / 90f) * Time.fixedDeltaTime;
        }

        float speed = lastMove.magnitude;
        lastMove += steerInput * steerStrength;
        lastMove = Vector3.ClampMagnitude(lastMove, Mathf.Min(speed, maxSlideSpeed));

        rb.linearVelocity = new Vector3(lastMove.x, rb.linearVelocity.y, lastMove.z);
        currentVelocity = new Vector3(lastMove.x, 0, lastMove.z);

        if (currentVelocity.magnitude < 0.5f) sliding = false;
    }

    private void HandleGroundedMovement(Vector3 inputDir, bool effectivelyGrounded, bool wantsCrouch)
    {
        if (effectivelyGrounded)
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

        inputDir = Vector3.ClampMagnitude(inputDir, 1f);
        float speedTarget = (wantsCrouch ? crouchSpeed : playerSpeed) + SpeedBonus;
        Vector3 targetVelocity = inputDir * speedTarget;

        float ramp = effectivelyGrounded ? accelTime : Mathf.Max(0.05f, airAccelTime - AirControlBonus);
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, (playerSpeed / ramp) * Time.fixedDeltaTime);

        lastMove = currentVelocity;
        rb.MovePosition(rb.position + lastMove * Time.fixedDeltaTime);
    }

    private void HandleJump()
    {
        if (grounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else if (airJumpsUsed < ExtraJumps)
        {
            airJumpsUsed++;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * airJumpForce, ForceMode.Impulse);
        }
    }

    public void Teleport(Vector3 pos)
    {
        rb.position = pos;
        rb.linearVelocity = Vector3.zero;
        currentVelocity = Vector3.zero;
        lastMove = Vector3.zero;
        transform.position = pos;
    }

    void OnCollisionStay() { grounded = true; }
    void OnCollisionExit() { grounded = false; }
}