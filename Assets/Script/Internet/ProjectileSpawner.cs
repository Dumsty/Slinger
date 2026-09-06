using UnityEngine;
using Unity.Netcode;

public class ProjectileSpawner : NetworkBehaviour
{
    public GameObject fireballPrefab;
    public GameObject blindBoltPrefab;
    public float ProjectileForceBonus;
    public float ProjectileSizeBonus;
    private Collider ownerCollider;

    public override void OnNetworkSpawn()
    {
        ownerCollider = GetComponent<Collider>();
        if (ownerCollider == null) ownerCollider = GetComponentInParent<Collider>();
        if (ownerCollider == null) ownerCollider = GetComponentInChildren<Collider>();
    }

    public static ProjectileSpawner Local()
    {
        foreach (var ps in FindObjectsByType<ProjectileSpawner>(FindObjectsSortMode.None))
            if (ps.IsOwner) return ps;
        return null;
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
        Collider ballCollider = ball.GetComponent<Collider>();

        if (explode == null || rb == null)
        {
            Destroy(ball);
            return;
        }

        explode.damage = damage;
        explode.ownerId = OwnerClientId;

        rb.useGravity = false;
        if (ballCollider != null && ownerCollider != null)
            Physics.IgnoreCollision(ballCollider, ownerCollider);

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
        Collider boltCollider = bolt.GetComponent<Collider>();

        if (explode == null || rb == null)
        {
            Destroy(bolt);
            return;
        }

        explode.ownerId = OwnerClientId;

        rb.useGravity = false;
        if (boltCollider != null && ownerCollider != null)
            Physics.IgnoreCollision(boltCollider, ownerCollider);

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