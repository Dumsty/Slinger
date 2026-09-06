using UnityEngine;

public class AirControlCard : Card
{
    public float duration = 10f;
    public float airControlBonus = 0.5f;

    public override void Play()
    {
        PlayerMovement pm = FindLocalPlayerMovement();
        BuffManager bm = BuffManager.Local();
        if (bm == null || pm == null) return;

        bool wasActive = bm.IsActive("aircontrol");
        bm.AddBuff("aircontrol", icon, duration);
        if (!wasActive) pm.StartCoroutine(GrantAirControlBoost(pm, bm));
    }

    PlayerMovement FindLocalPlayerMovement()
    {
        foreach (var p in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    System.Collections.IEnumerator GrantAirControlBoost(PlayerMovement pm, BuffManager bm)
    {
        pm.AirControlBonus += airControlBonus;
        while (bm != null && bm.IsActive("aircontrol"))
            yield return null;
        pm.AirControlBonus -= airControlBonus;
    }
}