using UnityEngine;

public class FireballCard : Card
{
    public GameObject ballPrefab;
    public float force = 20f;
    private Collider playerCollider;

    void Awake()
    {
        playerCollider = FindAnyObjectByType<Health>().GetComponent<Collider>();
    }

    public override void Play()
    {
        GameObject ball = Instantiate(ballPrefab, Camera.main.transform.position, Quaternion.identity);
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.AddForce(Camera.main.transform.forward * force, ForceMode.Impulse);
        Physics.IgnoreCollision(ball.GetComponent<Collider>(), playerCollider);
    }
}