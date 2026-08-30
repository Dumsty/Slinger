using UnityEngine;

public class LightningCard : Card
{
    public float range = 50f;
    public LineRenderer boltPrefab;
    public float boltDuration = 0.1f;
    public Vector3 originOffset;
    public float damage = 10f;

    public override void Play()
    {
        Transform cam = Camera.main.transform;
        Vector3 origin = cam.position + cam.TransformDirection(originOffset);
        Vector3 endPoint = origin + cam.forward * range;

        if (Physics.Raycast(origin, cam.forward, out RaycastHit hit, range))
        {
            endPoint = hit.point;
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(damage);
        }

        LineRenderer bolt = Instantiate(boltPrefab);
        bolt.SetPosition(0, origin);
        bolt.SetPosition(1, endPoint);
        Destroy(bolt.gameObject, boltDuration);
    }
}