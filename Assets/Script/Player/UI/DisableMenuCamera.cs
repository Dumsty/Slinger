using UnityEngine;
using Unity.Netcode;

public class MenuCameraDisable : MonoBehaviour
{
    void Update()
    {
        if (NetworkManager.Singleton.IsListening)
            gameObject.SetActive(false);
    }
}