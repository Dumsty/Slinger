using UnityEngine;
using Unity.Netcode;

public class PlayerCameraSetup : NetworkBehaviour
{
    public Camera playerCamera;
    public AudioListener audioListener;

    void Start()
    {
        playerCamera.enabled = IsOwner;
        audioListener.enabled = IsOwner;
    }
}