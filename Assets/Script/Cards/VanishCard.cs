using UnityEngine;

/// <summary>
/// While active, taking a single hit of damageThreshold or more triggers
/// temporary invisibility. Opens a window (duration) during which this can
/// trigger once; the invisibility itself runs independently of the window
/// closing, same pattern as RetaliateCard.
/// </summary>
public class VanishCard : Card
{
    [Tooltip("How long the vanish window stays open, in seconds.")]
    public float duration = 10f;

    [Tooltip("Minimum single-hit damage required to trigger vanish.")]
    public float damageThreshold = 20f;

    [Tooltip("How long the player stays invisible once triggered.")]
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

    // No PlayerInvisibility.Local() exists yet, so scan for the owned instance.
    PlayerInvisibility FindLocalInvisibility()
    {
        foreach (var p in FindObjectsByType<PlayerInvisibility>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    // Polls health each frame and compares against the threshold, rather
    // than subscribing to OnValueChanged, so it can measure the size of a
    // single drop, not just whether one occurred.
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