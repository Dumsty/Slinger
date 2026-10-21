using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Settings screen for sensitivity, FOV, volume, fullscreen and resolution.
/// Reused in both the main menu and the in-game pause menu - only assign
/// pauseUI in the pause-menu instance and mainMenuUI in the main-menu
/// instance; Back() only touches whichever one is actually assigned.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsUI;
    public GameObject pauseUI;
    public GameObject mainMenuUI;
    public Slider sensitivitySlider;
    public Slider fovSlider;

    [Header("Audio")]
    public Slider volumeSlider;

    [Header("Display")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    // PlayerPrefs keys (DisplayBootstrap reads the same strings at launch)
    const string FullscreenKey = "Fullscreen";
    const string ResWKey = "ResW";
    const string ResHKey = "ResH";
    const string VolumeKey = "Volume";

    struct ResPreset
    {
        public string label; public int w, h;
        public ResPreset(string label, int w, int h) { this.label = label; this.w = w; this.h = h; }
    }

    static readonly ResPreset[] presets =
    {
        new ResPreset("720p",             1280,  720),
        new ResPreset("720p Ultrawide",   1680,  720),
        new ResPreset("1080p",            1920, 1080),
        new ResPreset("1080p Ultrawide",  2560, 1080),
        new ResPreset("1440p",            2560, 1440),
        new ResPreset("1440p Ultrawide",  3440, 1440),
    };

    // Parallel to the dropdown entries, so index -> actual resolution
    readonly List<Vector2Int> resOptions = new List<Vector2Int>();

    void OnEnable()
    {
        // Show the real current state without firing the callbacks,
        // then (re)hook the listeners so they never get added twice.
        if (fullscreenToggle != null)
        {
            bool isFull = Screen.fullScreenMode != FullScreenMode.Windowed;
            fullscreenToggle.SetIsOnWithoutNotify(isFull);
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (volumeSlider != null)
        {
            // Prefer the saved value over whatever AudioListener currently holds,
            // so scene-load order can't hand us the default of 1.
            if (PlayerPrefs.HasKey(VolumeKey))
                AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey);

            volumeSlider.onValueChanged.RemoveListener(SetVolume);   // detach before touching range
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        BuildResolutionOptions();
    }

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

    // Sets master volume for all game audio and remembers it for next launch.
    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    // Toggles fullscreen and remembers the choice for the next launch.
    public void SetFullscreen(bool on)
    {
        Screen.fullScreenMode = on ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        PlayerPrefs.SetInt(FullscreenKey, on ? 1 : 0);
        PlayerPrefs.Save();
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

    // Rebuilds the list each time the menu opens so it always reflects the
    // current display and the resolution the game is actually running at.
    void BuildResolutionOptions()
    {
        if (resolutionDropdown == null) return;

        resOptions.Clear();
        var labels = new List<string>();

        int nativeW = Display.main.systemWidth;
        int nativeH = Display.main.systemHeight;

        foreach (var p in presets)
        {
            if (p.w > nativeW || p.h > nativeH) continue;   // too big for this monitor
            resOptions.Add(new Vector2Int(p.w, p.h));
            labels.Add(p.label);   // "1080p", "1080p Ultrawide", ...
        }

        // Detect the current resolution and select it
        int cw = Screen.width, ch = Screen.height;
        int index = resOptions.FindIndex(o => o.x == cw && o.y == ch);
        if (index < 0)
        {
            resOptions.Insert(0, new Vector2Int(cw, ch));
            labels.Insert(0, $"{cw} x {ch}");
            index = 0;
        }

        resolutionDropdown.onValueChanged.RemoveListener(SetResolution);
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.SetValueWithoutNotify(index);
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= resOptions.Count) return;
        Vector2Int r = resOptions[index];

        Screen.SetResolution(r.x, r.y, Screen.fullScreenMode);
        PlayerPrefs.SetInt(ResWKey, r.x);
        PlayerPrefs.SetInt(ResHKey, r.y);
        PlayerPrefs.Save();
    }
}