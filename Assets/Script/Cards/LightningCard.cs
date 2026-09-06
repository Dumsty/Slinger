using UnityEngine;
using Unity.Netcode;

public class LightningCard : Card
{
    public float range = 50f;
    public static LineRenderer boltPrefab;
    public LineRenderer boltPrefabRef;
    public float boltDuration = 0.1f;
    public Vector3 originOffset;
    public float damage = 10f;

    void Awake()
    {
        boltPrefab = boltPrefabRef;
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

        ProjectileSpawner.Local().FireLightning(origin, endPoint);
    }

    public static void SpawnVisual(Vector3 origin, Vector3 endPoint)
    {
        LineRenderer bolt = Instantiate(boltPrefab);
        bolt.SetPosition(0, origin);
        bolt.SetPosition(1, endPoint);
        Object.Destroy(bolt.gameObject, 0.1f);
    }
}