using UnityEngine;

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

    PlayerInvisibility FindLocalInvisibility()
    {
        foreach (var p in FindObjectsByType<PlayerInvisibility>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }
}