using UnityEngine;
using UnityEngine.Rendering;

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