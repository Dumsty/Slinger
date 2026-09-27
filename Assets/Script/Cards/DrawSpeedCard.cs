using UnityEngine;

/// <summary>
/// Reduces the time between card draws for a fixed duration when played.
/// </summary>
public class DrawSpeedCard : Card
{
    [Tooltip("Buff duration in seconds.")]
    public float duration = 10f;

    [Tooltip("Subtracted from the base draw fill time while active.")]
    public float speedBonus = 2f;

    public override void Play()
    {
        CardCountdown cc = CardCountdown.Local();
        BuffManager bm = BuffManager.Local();
        if (bm == null || cc == null) return;

        bool wasActive = bm.IsActive("drawspeed");
        bm.AddBuff("drawspeed", icon, duration);
        if (!wasActive) cc.StartCoroutine(GrantDrawSpeedBoost(cc, bm));
    }

    System.Collections.IEnumerator GrantDrawSpeedBoost(CardCountdown cc, BuffManager bm)
    {
        cc.DrawSpeedBonus += speedBonus;
        while (bm != null && bm.IsActive("drawspeed"))
            yield return null;
        cc.DrawSpeedBonus -= speedBonus;
    }
}