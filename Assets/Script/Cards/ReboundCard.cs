using UnityEngine;

/// <summary>
/// While active, the next hit(s) taken grant a temporary speed boost.
/// Opens a window (duration) during which any drop in health triggers
/// the boost; each trigger is independent of the window closing.
/// </summary>
public class RetaliateCard : Card
{
    [Tooltip("How long the retaliate window stays open, in seconds.")]
    public float duration = 10f;

    [Tooltip("Move speed bonus granted when triggered.")]
    public float speedBonus = 5f;

    [Tooltip("How long the speed boost lasts once triggered.")]
    public float boostDuration = 3f;

    public override void Play()
    {
        PlayerMovement pm = FindLocalPlayerMovement();
        Health health = pm != null ? pm.GetComponent<Health>() : null;
        BuffManager bm = BuffManager.Local();
        if (bm == null || pm == null || health == null) return;

        bm.AddBuff("retaliate", icon, duration);
        pm.StartCoroutine(WatchForDamage(pm, health, bm));
    }

    // No PlayerMovement.Local() exists yet, so scan for the owned instance.
    PlayerMovement FindLocalPlayerMovement()
    {
        foreach (var p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    // Polls health each frame rather than subscribing to OnValueChanged so
    // healing (an increase) is naturally ignored - only decreases count
    // as damage.
    System.Collections.IEnumerator WatchForDamage(PlayerMovement pm, Health health, BuffManager bm)
    {
        float lastHealth = health.currentHealth.Value;
        bool boosting = false;

        while (bm.IsActive("retaliate"))
        {
            float current = health.currentHealth.Value;
            if (!boosting && current < lastHealth)
            {
                // boosting flag prevents re-triggering while one is already
                // running - rapid multi-hit damage won't stack the bonus.
                boosting = true;
                pm.StartCoroutine(ApplySpeedBoost(pm, () => boosting = false));
            }
            lastHealth = current;
            yield return null;
        }
    }

    // Runs independently of the outer watch window, so a hit taken right
    // as the window closes still gets the full boost duration.
    System.Collections.IEnumerator ApplySpeedBoost(PlayerMovement pm, System.Action onComplete)
    {
        pm.SpeedBonus += speedBonus;
        yield return new WaitForSeconds(boostDuration);
        pm.SpeedBonus -= speedBonus;
        onComplete();
    }
}