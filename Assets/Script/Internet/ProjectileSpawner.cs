using UnityEngine;
using Unity.Netcode;

public class ProjectileSpawner : NetworkBehaviour
{
    public GameObject fireballPrefab;

    public static ProjectileSpawner Local()
    {
        foreach (var ps in FindObjectsByType<ProjectileSpawner>(FindObjectsSortMode.None))
            if (ps.IsOwner) return ps;
        return null;
    }

    public void FireFireball(Vector3 position, Vector3 direction, float force, float damage)
    {
        SpawnFireballServerRpc(position, direction, force, damage);
    }

    [ServerRpc]
    void SpawnFireballServerRpc(Vector3 position, Vector3 direction, float force, float damage)
    {
        Vector3 spawnPos = position + direction * 1.5f;
        GameObject ball = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        FireBallExplode explode = ball.GetComponent<FireBallExplode>();
        explode.damage = damage;
        explode.ownerId = OwnerClientId;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.useGravity = false;
        Physics.IgnoreCollision(ball.GetComponent<Collider>(), GetComponent<Collider>());
        ball.GetComponent<NetworkObject>().Spawn();
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