using UnityEngine;

public class LifeTapCard : Card
{
    public float damage = 10f;
    private Health playerHealth;

    public override void Play()
    {
        if (playerHealth == null) playerHealth = FindLocalHealth();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(damage, playerHealth.OwnerClientId);
    }

    Health FindLocalHealth()
    {
        foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
            if (h.IsOwner) return h;
        return null;
    }
}