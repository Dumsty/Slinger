using UnityEngine;

public class MoveSpeedCard : Card
{
    public float duration = 10f;
    public float speedBonus = 3f;

    public override void Play()
    {
        PlayerMovement pm = FindLocalPlayerMovement();
        BuffManager bm = BuffManager.Local();
        if (bm == null || pm == null) return;

        bool wasActive = bm.IsActive("movespeed");
        bm.AddBuff("movespeed", icon, duration);
        if (!wasActive) pm.StartCoroutine(GrantSpeedBoost(pm, bm));
    }

    PlayerMovement FindLocalPlayerMovement()
    {
        foreach (var p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    System.Collections.IEnumerator GrantSpeedBoost(PlayerMovement pm, BuffManager bm)
    {
        pm.SpeedBonus += speedBonus;
        while (bm != null && bm.IsActive("movespeed"))
            yield return null;
        pm.SpeedBonus -= speedBonus;
    }
}