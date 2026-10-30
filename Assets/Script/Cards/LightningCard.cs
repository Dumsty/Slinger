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
    // method can instantiate the prefabs without an instance reference.
    public static GameObject boltPrefab;
    public static GameObject impactPrefab;          // ADDED
    private static float sharedBoltDuration;

    [Tooltip("Bolt visual prefab (LineRenderer may be on the root or a child).")]
    public GameObject boltPrefabRef;

    [Tooltip("Effect spawned where the bolt lands.")]
    public GameObject impactPrefabRef;              // ADDED

    [Tooltip("How long the bolt visual stays on screen.")]
    public float boltDuration = 0.1f;

    [Tooltip("Offset from the camera the bolt appears to originate from.")]
    public Vector3 originOffset;

    [Tooltip("Damage dealt to whatever the raycast hits.")]
    public float damage = 10f;

    void Awake()
    {
        boltPrefab = boltPrefabRef;
        impactPrefab = impactPrefabRef;             // ADDED
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
        if (boltPrefab == null) return;

        // Spawn at the origin so any child particles/light/audio on the
        // prefab land at the caster's end of the bolt.
        GameObject bolt = Instantiate(boltPrefab, origin, Quaternion.LookRotation(
            (endPoint - origin).sqrMagnitude > 0.0001f
                ? (endPoint - origin).normalized
                : Vector3.forward));

        // The LineRenderer may sit on the root or on a child of the prefab.
        LineRenderer line = bolt.GetComponentInChildren<LineRenderer>();
        if (line != null)
        {
            line.useWorldSpace = true;      // positions below are world-space
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, endPoint);
        }
        else
        {
            Debug.LogWarning("LightningCard: bolt prefab has no LineRenderer.");
        }

        Object.Destroy(bolt, sharedBoltDuration);

        ImpactEffect.Play(impactPrefab, endPoint);  // ADDED
    }
}