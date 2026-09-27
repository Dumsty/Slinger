using UnityEngine;

/// <summary>
/// Heals the local player by a fixed amount when played.
/// </summary>
public class HealCard : Card
{
    [Tooltip("Amount of health restored.")]
    public float healAmount = 10f;

    private Health playerHealth;

    public override void Play()
    {
        if (playerHealth == null) playerHealth = FindLocalHealth();
        if (playerHealth == null) return;

        // Health has no dedicated Heal method, so this reuses TakeDamage
        // with a negative amount. Passing our own OwnerClientId as the
        // "attacker" since this is self-inflicted (healing).
        playerHealth.TakeDamage(-healAmount, playerHealth.OwnerClientId);
    }

    // No Health.Local() exists yet, so scan for the owned instance.
    Health FindLocalHealth()
    {
        foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
            if (h.IsOwner) return h;
        return null;
    }
}