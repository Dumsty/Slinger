using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Shows the HUD only once a network session is active (host started or
/// client connected) - hidden otherwise, e.g. while still in the main menu.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public GameObject hud;

    void Update()
    {
        if (NetworkManager.Singleton != null)
            hud.SetActive(NetworkManager.Singleton.IsListening);
    }
}