using UnityEngine;

public class ProjectileSizeCard : Card
{
    public float duration = 10f;
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