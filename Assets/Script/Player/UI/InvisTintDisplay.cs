using UnityEngine;
using UnityEngine.UI;

public class InvisTintDisplay : MonoBehaviour
{
    public Image tintOverlay;
    private PlayerInvisibility invisibility;

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

    PlayerInvisibility FindLocalInvisibility()
    {
        foreach (var p in FindObjectsByType<PlayerInvisibility>(FindObjectsSortMode.None))
            if (p.IsOwner) return p;
        return null;
    }
}