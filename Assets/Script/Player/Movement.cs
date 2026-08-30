using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField] private float playerSpeed;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float slideDrag = 0.5f;
    [SerializeField] private float steerStrength = 2f;
    [SerializeField] private float slopeAccel = 10f;
    private bool grounded;
    private Vector3 lastMove;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.C))
        {
            Vector3 steer = Camera.main.transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));
            steer.y = 0;
            lastMove = Vector3.Lerp(lastMove, Vector3.zero, slideDrag * Time.fixedDeltaTime);
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
            {
                Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                lastMove += slopeDir * slopeAccel * Time.fixedDeltaTime;
            }
            float speed = lastMove.magnitude;
            lastMove += steer * steerStrength;
            lastMove = Vector3.ClampMagnitude(lastMove, speed);
            rb.linearVelocity = new Vector3(lastMove.x, rb.linearVelocity.y, lastMove.z);
            return;
        }

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

        Vector3 movement = Camera.main.transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));
        movement.y = 0;
        movement = Vector3.ClampMagnitude(movement, 1f);
        lastMove = movement * playerSpeed;
        rb.MovePosition(rb.position + lastMove * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (grounded && Input.GetButtonDown("Jump"))
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void OnCollisionStay() { grounded = true; }
    void OnCollisionExit() { grounded = false; }
}