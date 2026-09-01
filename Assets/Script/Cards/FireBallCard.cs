using UnityEngine;

public class FireballCard : Card
{
    public float force = 20f;
    public float damage = 10f;

    public override void Play()
    {
        Transform cam = Camera.main.transform;
        ProjectileSpawner spawner = ProjectileSpawner.Local();
        Debug.Log("Firing via spawner on " + spawner.gameObject.name + " OwnerClientId=" + spawner.OwnerClientId);
        spawner.FireFireball(cam.position, cam.forward, force, damage);
    }
}