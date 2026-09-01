using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsUI;
    public GameObject pauseUI;
    public Slider sensitivitySlider;
    public Slider fovSlider;

    void Start()
    {
        sensitivitySlider.value = CameraMouseCon.mouseSensitivity;
    }

    void Update()
    {
        if (Camera.main != null && fovSlider.value == 0)
            fovSlider.value = Camera.main.fieldOfView;
    }

    public void SetSensitivity(float value)
    {
        CameraMouseCon.mouseSensitivity = value;
    }

    public void SetFOV(float value)
    {
        if (Camera.main != null) Camera.main.fieldOfView = value;
    }

    public void Back()
    {
        settingsUI.SetActive(false);
        pauseUI.SetActive(true);
    }
}