using UnityEngine;

public class ProjectileSpeedCard : Card
{
    public float duration = 10f;
    public float forceBonus = 15f;

    public override void Play()
    {
        ProjectileSpawner spawner = ProjectileSpawner.Local();
        BuffManager bm = BuffManager.Local();
        if (bm == null || spawner == null) return;

        bool wasActive = bm.IsActive("projectilespeed");
        bm.AddBuff("projectilespeed", icon, duration);
        if (!wasActive) spawner.StartCoroutine(GrantProjectileSpeedBoost(spawner, bm));
    }

    System.Collections.IEnumerator GrantProjectileSpeedBoost(ProjectileSpawner spawner, BuffManager bm)
    {
        spawner.ProjectileForceBonus += forceBonus;
        while (bm != null && bm.IsActive("projectilespeed"))
            yield return null;
        spawner.ProjectileForceBonus -= forceBonus;
    }
}