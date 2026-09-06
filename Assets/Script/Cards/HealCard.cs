using UnityEngine;

public class HealCard : Card
{
    public float healAmount = 10f;
    private Health playerHealth;

    public override void Play()
    {
        if (playerHealth == null) playerHealth = FindLocalHealth();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(-healAmount, playerHealth.OwnerClientId);
    }

    Health FindLocalHealth()
    {
        foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
            if (h.IsOwner) return h;
        return null;
    }
}