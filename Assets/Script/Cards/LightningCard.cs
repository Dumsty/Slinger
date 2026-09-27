using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Fires an instant hitscan lightning bolt: damage is resolved locally via
/// raycast, while a ClientRpc (through ProjectileSpawner) shows the visual
/// bolt on every client.
/// </summary>
public class LightningCard : Card
{
    [Tooltip("Max range of the lightning raycast.")]
    public float range = 50f;

    // Shared across all LightningCard instances so the static SpawnVisual
    // method can instantiate the bolt prefab without an instance reference.
    public static LineRenderer boltPrefab;
    private static float sharedBoltDuration;

    [Tooltip("Bolt visual prefab, assigned per-card in the Inspector.")]
    public LineRenderer boltPrefabRef;

    [Tooltip("How long the bolt visual stays on screen.")]
    public float boltDuration = 0.1f;

    [Tooltip("Offset from the camera the bolt appears to originate from.")]
    public Vector3 originOffset;

    [Tooltip("Damage dealt to whatever the raycast hits.")]
    public float damage = 10f;

    void Awake()
    {
        boltPrefab = boltPrefabRef;
        sharedBoltDuration = boltDuration;
    }

    public override void Play()
    {
        Transform cam = Camera.main.transform;
        Vector3 origin = cam.position + cam.TransformDirection(originOffset);
        Vector3 endPoint = origin + cam.forward * range;
        ulong casterId = NetworkManager.Singleton.LocalClientId;

        if (Physics.Raycast(origin, cam.forward, out RaycastHit hit, range))
        {
            endPoint = hit.point;

            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(damage);

            Health health = hit.collider.GetComponent<Health>();
            if (health != null) health.TakeDamage(damage, casterId);
        }

        ProjectileSpawner spawner = ProjectileSpawner.Local();
        if (spawner == null) return;

        // Networked purely for the visual - damage above is applied locally.
        spawner.FireLightning(origin, endPoint);
    }

    // Called via ClientRpc from ProjectileSpawner on every client to show
    // the bolt, then destroys itself after sharedBoltDuration.
    public static void SpawnVisual(Vector3 origin, Vector3 endPoint)
    {
        LineRenderer bolt = Instantiate(boltPrefab);
        bolt.SetPosition(0, origin);
        bolt.SetPosition(1, endPoint);
        Object.Destroy(bolt.gameObject, sharedBoltDuration);
    }
}