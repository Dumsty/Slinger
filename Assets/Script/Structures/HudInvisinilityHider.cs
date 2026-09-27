using UnityEngine;

/// <summary>
/// Hides the HUD while the local player is invisible. Guarded against
/// pause/shop states so it doesn't fight PauseMenu/DeckStation, which also
/// control hud's active state for their own reasons.
/// </summary>
public class HudInvisibilityHider : MonoBehaviour
{
    public GameObject hud;
    private PlayerInvisibility invisibility;

    void Update()
    {
        if (PauseMenu.paused) return;
        if (DeckStation.editingDeck) return;

        if (invisibility == null)
        {
            invisibility = FindLocalInvisibility();
            return;
        }

        if (hud != null)
            hud.SetActive(!invisibility.IsInvisible);
    }

    // No PlayerInvisibility.Local() exists yet, so scan for the owned instance.
    PlayerInvisibility FindLocalInvisibility()
    {
        foreach (var p in FindObjectsByType<PlayerInvisibility>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }
}