using UnityEngine;

/// <summary>
/// Fires a damaging projectile in the direction the player is looking.
/// </summary>
public class FireballCard : Card
{
    [Tooltip("Launch force applied to the fireball.")]
    public float force = 20f;

    [Tooltip("Damage dealt on impact.")]
    public float damage = 10f;

    public override void Play()
    {
        Transform cam = Camera.main.transform;
        ProjectileSpawner spawner = ProjectileSpawner.Local();
        if (spawner == null) return;

        spawner.FireFireball(cam.position, cam.forward, force, damage);
    }
}