using UnityEngine;

public class RetaliateCard : Card
{
    public float duration = 10f;
    public float speedBonus = 5f;
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

    PlayerMovement FindLocalPlayerMovement()
    {
        foreach (var p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    System.Collections.IEnumerator WatchForDamage(PlayerMovement pm, Health health, BuffManager bm)
    {
        float lastHealth = health.currentHealth.Value;
        bool boosting = false;

        while (bm.IsActive("retaliate"))
        {
            float current = health.currentHealth.Value;
            if (!boosting && current < lastHealth)
            {
                boosting = true;
                pm.StartCoroutine(ApplySpeedBoost(pm, () => boosting = false));
            }
            lastHealth = current;
            yield return null;
        }
    }

    System.Collections.IEnumerator ApplySpeedBoost(PlayerMovement pm, System.Action onComplete)
    {
        pm.SpeedBonus += speedBonus;
        yield return new WaitForSeconds(boostDuration);
        pm.SpeedBonus -= speedBonus;
        onComplete();
    }
}