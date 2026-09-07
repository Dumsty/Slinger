using UnityEngine;
using Unity.Netcode;

public class FireBallExplode : NetworkBehaviour
{
    public float damage = 10f;
    public float range = 30f;
    public NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(ulong.MaxValue);
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (Vector3.Distance(startPos, transform.position) >= range)
            TryDestroy();
    }

    void OnCollisionEnter(Collision col)
    {
        NetworkObject netObj = col.gameObject.GetComponent<NetworkObject>();

        if (netObj != null && netObj.OwnerClientId == ownerId.Value) return;
        if (!IsServer) return;

        Enemy enemy = col.gameObject.GetComponent<Enemy>();
        if (enemy != null) enemy.TakeDamage(damage);

        Health health = col.gameObject.GetComponent<Health>();
        if (health != null) health.TakeDamage(damage, ownerId.Value);

        TryDestroy();
    }

    void TryDestroy()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer)
                netObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}