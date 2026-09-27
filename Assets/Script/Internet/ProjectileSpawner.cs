using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-authoritative spawner for all player-fired projectiles.
/// Client cards call the public Fire* methods; actual spawning happens
/// via ServerRpc so the server stays authoritative over damage/effects.
/// </summary>
public class ProjectileSpawner : NetworkBehaviour
{
    public GameObject fireballPrefab;
    public GameObject blindBoltPrefab;

    // Temporary bonuses applied by buff cards (ProjectileSpeedCard, etc.).
    public float ProjectileForceBonus;
    public float ProjectileSizeBonus;

    private Collider[] ownerColliders;

    public override void OnNetworkSpawn()
    {
        // Collect every collider on the owning player so projectiles can
        // be told to ignore all of them, not just one.
        ownerColliders = GetComponentsInChildren<Collider>();
        if (ownerColliders.Length == 0)
        {
            Collider parentCollider = GetComponentInParent<Collider>();
            if (parentCollider != null) ownerColliders = new Collider[] { parentCollider };
        }
    }

    public static ProjectileSpawner Local()
    {
        foreach (var ps in FindObjectsByType<ProjectileSpawner>(FindObjectsSortMode.None))
            if (ps.IsOwner) return ps;
        return null;
    }

    // Ensures a freshly spawned projectile can never collide with the
    // player who fired it, regardless of how many colliders either side has.
    private void IgnoreOwnerCollisions(GameObject projectile)
    {
        if (ownerColliders == null) return;

        Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>();
        foreach (var oc in ownerColliders)
            foreach (var pc in projectileColliders)
                if (oc != null && pc != null) Physics.IgnoreCollision(pc, oc);
    }

    public void FireFireball(Vector3 position, Vector3 direction, float force, float damage)
    {
        SpawnFireballServerRpc(position, direction, force + ProjectileForceBonus, damage, 1f + ProjectileSizeBonus);
    }

    [ServerRpc]
    void SpawnFireballServerRpc(Vector3 position, Vector3 direction, float force, float damage, float scale)
    {
        Vector3 spawnPos = position + direction * 1.5f;
        GameObject ball = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        ball.transform.localScale *= scale;

        FireBallExplode explode = ball.GetComponent<FireBallExplode>();
        Rigidbody rb = ball.GetComponent<Rigidbody>();

        if (explode == null || rb == null)
        {
            Destroy(ball);
            return;
        }

        explode.damage = damage;
        explode.ownerId.Value = OwnerClientId;

        rb.useGravity = false;
        IgnoreOwnerCollisions(ball);

        // Spawn before applying force so the NetworkObject exists on all
        // clients with its correct starting position/scale first.
        ball.GetComponent<NetworkObject>().Spawn();
        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void FireBlindBolt(Vector3 position, Vector3 direction, float force)
    {
        SpawnBlindBoltServerRpc(position, direction, force + ProjectileForceBonus, 1f + ProjectileSizeBonus);
    }

    [ServerRpc]
    void SpawnBlindBoltServerRpc(Vector3 position, Vector3 direction, float force, float scale)
    {
        Vector3 spawnPos = position + direction * 1.5f;
        GameObject bolt = Instantiate(blindBoltPrefab, spawnPos, Quaternion.identity);
        bolt.transform.localScale *= scale;

        BlindBoltExplode explode = bolt.GetComponent<BlindBoltExplode>();
        Rigidbody rb = bolt.GetComponent<Rigidbody>();

        if (explode == null || rb == null)
        {
            Destroy(bolt);
            return;
        }

        explode.ownerId.Value = OwnerClientId;

        rb.useGravity = false;
        IgnoreOwnerCollisions(bolt);

        bolt.GetComponent<NetworkObject>().Spawn();
        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void FireLightning(Vector3 origin, Vector3 end)
    {
        FireLightningServerRpc(origin, end);
    }

    // Lightning damage is resolved locally on the caster via raycast
    // (see LightningCard) - this RPC only exists to broadcast the visual
    // bolt to every client.
    [ServerRpc]
    void FireLightningServerRpc(Vector3 origin, Vector3 end)
    {
        ShowLightningClientRpc(origin, end);
    }

    [ClientRpc]
    void ShowLightningClientRpc(Vector3 origin, Vector3 end)
    {
        LightningCard.SpawnVisual(origin, end);
    }
}