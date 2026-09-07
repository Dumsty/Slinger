using UnityEngine;
using Unity.Netcode;

public class ProjectileSpawner : NetworkBehaviour
{
    public GameObject fireballPrefab;
    public GameObject blindBoltPrefab;
    public float ProjectileForceBonus;
    public float ProjectileSizeBonus;
    private Collider[] ownerColliders;

    public override void OnNetworkSpawn()
    {
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