using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseUI;
    public GameObject settingsUI;
    public GameObject hud;
    public GameObject deckBuilderUI;
    public static bool paused;

    private bool pausedFromShop;

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (DeckStation.editingDeck)
            {
                CloseShopAndPause();
            }
            else if (paused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    void CloseShopAndPause()
    {
        pausedFromShop = true;
        if (deckBuilderUI != null) deckBuilderUI.SetActive(false);
        DeckStation.editingDeck = false;
        Pause();
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

        if (pausedFromShop)
        {
            pausedFromShop = false;
            if (deckBuilderUI != null) deckBuilderUI.SetActive(true);
            DeckStation.editingDeck = true;
            if (hud != null) hud.SetActive(false);
        }
        else
        {
            if (hud != null) hud.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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

        StartCoroutine(ShutdownAndReturnToMenu());
    }

    IEnumerator ShutdownAndReturnToMenu()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            while (NetworkManager.Singleton.ShutdownInProgress)
                yield return null;
        }

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