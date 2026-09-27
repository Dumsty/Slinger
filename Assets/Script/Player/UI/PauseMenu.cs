using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// In-game pause menu: Escape opens/closes it, or - if the player is
/// currently in the deck shop - closes the shop and opens pause instead,
/// remembering to return to the shop (not gameplay) on resume.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public GameObject pauseUI;
    public GameObject settingsUI;
    public GameObject hud;
    public GameObject deckBuilderUI;
    public static bool paused;

    // Tracks whether the current pause was triggered from inside the shop,
    // so Resume() knows whether to return to gameplay or back to the shop.
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
            // Re-open the shop instead of returning to normal gameplay -
            // cursor stays unlocked/visible, matching shop state.
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

    // Waits for NetworkManager.Shutdown() to fully complete before loading
    // the menu scene - loading immediately after calling Shutdown() can
    // race with Netcode still tearing down the old scene's network
    // objects, causing the two scenes' UI to briefly overlap.
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