using UnityEngine;

/// <summary>
/// Temporarily increases fired projectile size (and hitbox) for a fixed
/// duration when played.
/// </summary>
public class ProjectileSizeCard : Card
{
    [Tooltip("Buff duration in seconds.")]
    public float duration = 10f;

    [Tooltip("Added to projectile scale while active (0.5 = 50% bigger).")]
    public float sizeBonus = 0.5f;

    public override void Play()
    {
        ProjectileSpawner spawner = ProjectileSpawner.Local();
        BuffManager bm = BuffManager.Local();
        if (bm == null || spawner == null) return;

        bool wasActive = bm.IsActive("projectilesize");
        bm.AddBuff("projectilesize", icon, duration);
        if (!wasActive) spawner.StartCoroutine(GrantProjectileSizeBoost(spawner, bm));
    }

    System.Collections.IEnumerator GrantProjectileSizeBoost(ProjectileSpawner spawner, BuffManager bm)
    {
        spawner.ProjectileSizeBonus += sizeBonus;
        while (bm != null && bm.IsActive("projectilesize"))
            yield return null;
        spawner.ProjectileSizeBonus -= sizeBonus;
    }
}