using UnityEngine;

public class InvisibilityCard : Card
{
    public float duration = 8f;

    public override void Play()
    {
        PlayerInvisibility inv = FindLocalInvisibility();
        BuffManager bm = BuffManager.Local();
        if (bm == null || inv == null) return;

        bool wasActive = bm.IsActive("invisible");
        bm.AddBuff("invisible", icon, duration);
        if (!wasActive) inv.StartCoroutine(GrantInvisibility(inv, bm));
    }

    PlayerInvisibility FindLocalInvisibility()
    {
        foreach (var p in FindObjectsByType<PlayerInvisibility>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }

    System.Collections.IEnumerator GrantInvisibility(PlayerInvisibility inv, BuffManager bm)
    {
        inv.SetInvisible(true);
        while (bm != null && bm.IsActive("invisible"))
            yield return null;
        inv.SetInvisible(false);
    }
}