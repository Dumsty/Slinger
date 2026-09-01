using UnityEngine;

public class ShooterEnemy : Enemy
{
    public float wanderRadius = 10f;
    public float moveSpeed = 2f;
    public float shootRange = 15f;
    public float fireRate = 2f;
    public GameObject ballPrefab;
    public float force = 20f;
    public float maxDistanceFromSpawn = 15f;
    public float pauseDuration = 0.5f;

    private Vector3 wanderTarget;
    private Vector3 spawnPos;
    private float fireTimer;
    private float pauseTimer;
    private bool paused;
    private Transform player;

    protected override void Start()
    {
        base.Start();
        spawnPos = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        if (player == null)
        {
            Health h = FindAnyObjectByType<Health>();
            if (h == null) return;
            player = h.transform;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer <= shootRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate)
            {
                fireTimer = 0;
                Shoot();
            }
        }
        else
        {
            Wander();
        }
    }

    void Wander()
    {
        if (paused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0) paused = false;
            return;
        }

        Vector3 nextPos = Vector3.MoveTowards(transform.position, wanderTarget, moveSpeed * Time.deltaTime);
        if (Physics.Raycast(nextPos + Vector3.up * 0.5f, Vector3.down, 1.5f))
            transform.position = nextPos;
        else
            StartPause();

        if (Vector3.Distance(transform.position, wanderTarget) < 0.5f)
            StartPause();
    }

    void StartPause()
    {
        paused = true;
        pauseTimer = pauseDuration;
        PickNewTarget();
    }

    void PickNewTarget()
    {
        Vector3 candidate = transform.position;
        for (int i = 0; i < 20; i++)
        {
            candidate = transform.position + new Vector3(Random.Range(-wanderRadius, wanderRadius), 0, Random.Range(-wanderRadius, wanderRadius));
            if (Vector3.Distance(spawnPos, candidate) <= maxDistanceFromSpawn) break;
        }
        wanderTarget = candidate;
    }

    void Shoot()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        GameObject ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.AddForce(dir * force, ForceMode.Impulse);
        Physics.IgnoreCollision(ball.GetComponent<Collider>(), GetComponent<Collider>());
    }
}