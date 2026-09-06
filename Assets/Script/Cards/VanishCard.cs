using UnityEngine;

public class VanishCard : Card
{
    public float duration = 10f;
    public float damageThreshold = 20f;
    public float invisDuration = 5f;

    public override void Play()
    {
        PlayerInvisibility inv = FindLocalInvisibility();
        Health health = inv != null ? inv.GetComponent<Health>() : null;
        BuffManager bm = BuffManager.Local();
        if (bm == null || inv == null || health == null) return;

        bm.AddBuff("vanish", icon, duration);
        inv.StartCoroutine(WatchForBigHit(inv, health, bm));
    }

    PlayerInvisibility FindLocalInvisibility()
    {
        foreach (var p in FindObjectsByType<PlayerInvisibility>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    System.Collections.IEnumerator WatchForBigHit(PlayerInvisibility inv, Health health, BuffManager bm)
    {
        float lastHealth = health.currentHealth.Value;
        bool vanishing = false;

        while (bm.IsActive("vanish"))
        {
            float current = health.currentHealth.Value;
            float dropped = lastHealth - current;

            if (!vanishing && dropped >= damageThreshold)
            {
                vanishing = true;
                inv.StartCoroutine(ApplyVanish(inv, () => vanishing = false));
            }

            lastHealth = current;
            yield return null;
        }
    }

    System.Collections.IEnumerator ApplyVanish(PlayerInvisibility inv, System.Action onComplete)
    {
        inv.SetInvisible(true);
        yield return new WaitForSeconds(invisDuration);
        inv.SetInvisible(false);
        onComplete();
    }
}