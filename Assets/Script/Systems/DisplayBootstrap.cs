using UnityEngine;

/// <summary>
/// Applies the player's saved display and audio choices once at launch.
/// Lives in the main menu scene only.
/// </summary>
public class DisplayBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (PlayerPrefs.HasKey("Fullscreen"))
            Screen.fullScreenMode = PlayerPrefs.GetInt("Fullscreen") == 1
                ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        if (PlayerPrefs.HasKey("ResW") && PlayerPrefs.HasKey("ResH"))
            Screen.SetResolution(PlayerPrefs.GetInt("ResW"), PlayerPrefs.GetInt("ResH"), Screen.fullScreenMode);

        if (PlayerPrefs.HasKey("Volume"))
            AudioListener.volume = PlayerPrefs.GetFloat("Volume");
    }
}