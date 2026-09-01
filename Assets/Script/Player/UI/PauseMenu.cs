using UnityEngine;
using Unity.Netcode;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseUI;
    public GameObject settingsUI;
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        paused = false;
        pauseUI.SetActive(false);
        settingsUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        settingsUI.SetActive(true);
        pauseUI.SetActive(false);
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