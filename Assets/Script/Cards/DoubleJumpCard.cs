using UnityEngine;

public class DoubleJumpCard : Card
{
    public float duration = 10f;
    public int extraJumps = 1;
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

    PlayerMovement FindLocalPlayerMovement()
    {
        foreach (var p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    System.Collections.IEnumerator GrantDoubleJump(PlayerMovement pm)
    {
        pm.ExtraJumps += extraJumps;
        pm.airJumpForce = jumpForceAmount;
        BuffManager bm = BuffManager.Local();
        while (bm != null && bm.IsActive("doublejump"))
            yield return null;
        pm.ExtraJumps -= extraJumps;
    }
}