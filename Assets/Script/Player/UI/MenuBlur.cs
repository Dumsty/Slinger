using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Smoothly fades a post-process Volume's weight in/out based on whether
/// any of the assigned menu panels are currently open - used to blur the
/// background while a menu is showing. The same script works in both the
/// main menu and game scenes; menusToWatch is configured per-scene in the
/// Inspector.
/// </summary>
public class MenuBlur : MonoBehaviour
{
    public Volume blurVolume;
    public float fadeSpeed = 5f;
    public GameObject[] menusToWatch;

    void Update()
    {
        if (blurVolume == null) return;

        bool anyMenuOpen = false;
        foreach (var menu in menusToWatch)
            if (menu != null && menu.activeInHierarchy) { anyMenuOpen = true; break; }

        float target = anyMenuOpen ? 1f : 0f;
        blurVolume.weight = Mathf.MoveTowards(blurVolume.weight, target, fadeSpeed * Time.deltaTime);
    }
}