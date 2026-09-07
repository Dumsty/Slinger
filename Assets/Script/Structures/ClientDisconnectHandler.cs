using UnityEngine;
using Unity.Netcode;

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
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}