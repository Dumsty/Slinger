using UnityEngine;

/// <summary>
/// Grants extra mid-air jumps for a fixed duration when played.
/// </summary>
public class DoubleJumpCard : Card
{
    [Tooltip("Buff duration in seconds.")]
    public float duration = 10f;

    [Tooltip("Extra air jumps granted while active.")]
    public int extraJumps = 1;

    [Tooltip("Air jump force applied while the buff is active.")]
    public float jumpForceAmount = 5f;

    public override void Play()
    {
        PlayerMovement pm = FindLocalPlayerMovement();
        BuffManager bm = BuffManager.Local();
        if (bm == null || pm == null) return;

        bool wasActive = bm.IsActive("doublejump");
        bm.AddBuff("doublejump", icon, duration);
        if (!wasActive) pm.StartCoroutine(GrantDoubleJump(pm));
    }

    // No PlayerMovement.Local() exists yet, so scan for the owned instance.
    PlayerMovement FindLocalPlayerMovement()
    {
        foreach (var p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    System.Collections.IEnumerator GrantDoubleJump(PlayerMovement pm)
    {
        float originalJumpForce = pm.airJumpForce;

        pm.ExtraJumps += extraJumps;
        pm.airJumpForce = jumpForceAmount;

        BuffManager bm = BuffManager.Local();
        while (bm != null && bm.IsActive("doublejump"))
            yield return null;

        pm.ExtraJumps -= extraJumps;
        pm.airJumpForce = originalJumpForce;
    }
}