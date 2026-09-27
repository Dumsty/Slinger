using UnityEngine;

/// <summary>
/// Temporarily boosts the local player's air control for a fixed duration.
/// </summary>
public class AirControlCard : Card
{
    [Tooltip("Buff duration in seconds.")]
    public float duration = 10f;

    [Tooltip("Air acceleration bonus while active.")]
    public float airControlBonus = 0.5f;

    public override void Play()
    {
        PlayerMovement pm = FindLocalPlayerMovement();
        BuffManager bm = BuffManager.Local();
        if (bm == null || pm == null) return;

        // Refresh duration if already active instead of stacking coroutines.
        bool wasActive = bm.IsActive("aircontrol");
        bm.AddBuff("aircontrol", icon, duration);
        if (!wasActive) pm.StartCoroutine(GrantAirControlBoost(pm, bm));
    }

    // No PlayerMovement.Local() exists yet, so scan for the owned instance.
    PlayerMovement FindLocalPlayerMovement()
    {
        foreach (var p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    // Applies the bonus, waits for the buff to expire, then removes it.
    System.Collections.IEnumerator GrantAirControlBoost(PlayerMovement pm, BuffManager bm)
    {
        pm.AirControlBonus += airControlBonus;
        while (bm != null && bm.IsActive("aircontrol"))
            yield return null;
        pm.AirControlBonus -= airControlBonus;
    }
}