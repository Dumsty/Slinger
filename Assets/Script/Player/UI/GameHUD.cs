using UnityEngine;
using Unity.Netcode;

public class GameHUD : MonoBehaviour
{
    public GameObject hud;

    void Update()
    {
        hud.SetActive(NetworkManager.Singleton.IsListening);
    }
}