using UnityEngine;
using Unity.Netcode;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseUI;
    public GameObject settingsUI;
    public GameObject hud;
    public static bool paused;

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (paused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        paused = true;
        pauseUI.SetActive(true);
        if (hud != null) hud.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        paused = false;
        pauseUI.SetActive(false);
        settingsUI.SetActive(false);
        if (hud != null) hud.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("Resume() ran, cursor set to locked/hidden. Actual state: lockState=" + Cursor.lockState + " visible=" + Cursor.visible);
    }

    public void OpenSettings()
    {
        settingsUI.SetActive(true);
        pauseUI.SetActive(false);
    }

    public void QuitToMainMenu()
    {
        paused = false;
        DeckStation.editingDeck = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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