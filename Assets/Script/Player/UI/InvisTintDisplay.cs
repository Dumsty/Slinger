using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a light screen tint while the local player is invisible.
/// Important: this script must live on a different, always-active object
/// than tintOverlay itself - if attached directly to the overlay it
/// controls, hiding it would also stop this script's own Update() from
/// ever running again.
/// </summary>
public class InvisTintDisplay : MonoBehaviour
{
    public Image tintOverlay;
    private PlayerInvisibility invisibility;

    // Forces the overlay off immediately on scene load, closing the gap
    // before invisibility is resolved below (during which it would
    // otherwise show whatever state was saved in the scene).
    void Start()
    {
        if (tintOverlay != null)
            tintOverlay.gameObject.SetActive(false);
    }

    void Update()
    {
        if (invisibility == null)
        {
            invisibility = FindLocalInvisibility();
            return;
        }

        if (tintOverlay != null)
            tintOverlay.gameObject.SetActive(invisibility.IsInvisible);
    }

    // No PlayerInvisibility.Local() exists yet, so scan for the owned instance.
    PlayerInvisibility FindLocalInvisibility()
    {
        foreach (var p in FindObjectsByType<PlayerInvisibility>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }
}