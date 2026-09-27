using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Hides the main menu camera once the player has hosted or joined a
/// match, so the game scene's own camera takes over. Lives in the main
/// menu scene alongside the persistent NetworkManager.
/// </summary>
public class MenuCameraDisable : MonoBehaviour
{
    void Update()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            gameObject.SetActive(false);
    }
}