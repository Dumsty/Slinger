using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Projectile that deals damage on impact. Owner is stored in a
/// NetworkVariable (not a plain field) so it correctly syncs to every
/// client - without that, clients running their own local physics would
/// see the default/unset value instead of the real owner.
/// </summary>
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
        // GetComponentInParent rather than GetComponent, since the actual
        // collider hit may belong to a child hitbox rather than the
        // player's root object where NetworkObject/Health live.
        NetworkObject netObj = col.gameObject.GetComponentInParent<NetworkObject>();

        if (netObj != null && netObj.OwnerClientId == ownerId.Value) return;

        // Only the server applies damage - a client's own physics may
        // fire this same collision event locally, but shouldn't act on it.
        if (!IsServer) return;

        Enemy enemy = netObj != null ? netObj.GetComponentInChildren<Enemy>() : col.gameObject.GetComponent<Enemy>();
        if (enemy != null) enemy.TakeDamage(damage);

        Health health = netObj != null ? netObj.GetComponentInChildren<Health>() : col.gameObject.GetComponent<Health>();
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