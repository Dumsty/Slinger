using UnityEngine;

/// <summary>
/// Top-level main menu navigation: switches between the title screen,
/// settings, and duel (host/join) panels.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuUI;
    public GameObject settingsUI;
    public GameObject duelUI;

    public void OpenSettings()
    {
        settingsUI.SetActive(true);
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
    }

    public void OpenDuel()
    {
        duelUI.SetActive(true);
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
    }

    public void BackToMainMenu()
    {
        if (settingsUI != null) settingsUI.SetActive(false);
        if (duelUI != null) duelUI.SetActive(false);
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}