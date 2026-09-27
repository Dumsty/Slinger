using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Automatically returns a non-host client to the main menu if they get
/// disconnected (e.g. the host closes the session). The host has their
/// own explicit QuitToMainMenu() flow, so this only reacts for clients.
/// Lives on a persistent object alongside NetworkManager.
/// </summary>
public class ClientDisconnectHandler : MonoBehaviour
{
    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
    }

    void OnDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer) return;

        // OnClientDisconnectCallback can fire for other clients too - only
        // react when it's this client that was disconnected.
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}