using UnityEngine;

/// <summary>
/// Damages the local player by a fixed amount when played. Intended as
/// a risk/reward card - pair with another effect for the actual payoff.
/// </summary>
public class LifeTapCard : Card
{
    [Tooltip("Self-inflicted damage dealt.")]
    public float damage = 10f;

    private Health playerHealth;

    public override void Play()
    {
        if (playerHealth == null) playerHealth = FindLocalHealth();
        if (playerHealth == null) return;

        // Self-inflicted, so the attacker is our own OwnerClientId.
        playerHealth.TakeDamage(damage, playerHealth.OwnerClientId);
    }

    // No Health.Local() exists yet, so scan for the owned instance.
    Health FindLocalHealth()
    {
        foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
            if (h.IsOwner) return h;
        return null;
    }
}