using UnityEngine;
using UnityEngine.UI;

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
        CameraMouseCon local = FindLocalCameraMouseCon();
        if (local != null) local.ApplyFOV();
    }

    public void Back()
    {
        settingsUI.SetActive(false);
        if (pauseUI != null) pauseUI.SetActive(true);
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
    }

    CameraMouseCon FindLocalCameraMouseCon()
    {
        foreach (var c in FindObjectsByType<CameraMouseCon>(FindObjectsSortMode.None))
            if (c.IsOwner) return c;
        return null;
    }
}