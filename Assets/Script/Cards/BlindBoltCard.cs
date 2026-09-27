using UnityEngine;

/// <summary>
/// Fires a projectile that blinds the enemy player on impact.
/// </summary>
public class BlindBoltCard : Card
{
    [Tooltip("Launch force applied to the bolt.")]
    public float force = 20f;

    public override void Play()
    {
        Transform cam = Camera.main.transform;
        ProjectileSpawner spawner = ProjectileSpawner.Local();
        if (spawner == null) return;

        // Actual spawning/networking is handled server-side by ProjectileSpawner.
        spawner.FireBlindBolt(cam.position, cam.forward, force);
    }
}