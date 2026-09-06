using UnityEngine;

public class BlindBoltCard : Card
{
    public float force = 20f;

    public override void Play()
    {
        Transform cam = Camera.main.transform;
        ProjectileSpawner spawner = ProjectileSpawner.Local();
        if (spawner == null) return;

        spawner.FireBlindBolt(cam.position, cam.forward, force);
    }
}