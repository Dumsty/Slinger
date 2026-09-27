using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings screen for sensitivity and FOV. Reused in both the main menu
/// and the in-game pause menu - only assign pauseUI in the pause-menu
/// instance and mainMenuUI in the main-menu instance; Back() only touches
/// whichever one is actually assigned.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsUI;
    public GameObject pauseUI;
    public GameObject mainMenuUI;
    public Slider sensitivitySlider;
    public Slider fovSlider;

    void Start()
    {
        if (sensitivitySlider != null) sensitivitySlider.value = CameraMouseCon.mouseSensitivity;
        if (fovSlider != null) fovSlider.value = CameraMouseCon.fieldOfView;
    }

    public void SetSensitivity(float value)
    {
        CameraMouseCon.mouseSensitivity = value;
    }

    public void SetFOV(float value)
    {
        CameraMouseCon.fieldOfView = value;

        // Also apply immediately to the current player's camera (if one
        // exists), so the change is felt live rather than only on next spawn.
        CameraMouseCon local = FindLocalCameraMouseCon();
        if (local != null) local.ApplyFOV();
    }

    public void Back()
    {
        settingsUI.SetActive(false);
        if (pauseUI != null) pauseUI.SetActive(true);
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
    }

    // No CameraMouseCon.Local() exists yet, so scan for the owned instance.
    CameraMouseCon FindLocalCameraMouseCon()
    {
        foreach (var c in FindObjectsByType<CameraMouseCon>(FindObjectsSortMode.None))
            if (c.IsOwner) return c;
        return null;
    }
}