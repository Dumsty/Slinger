using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Enables the player's own camera and audio listener only for the owning
/// client - other players see this player's body, not their view.
/// </summary>
public class PlayerCameraSetup : NetworkBehaviour
{
    public Camera playerCamera;
    public AudioListener audioListener;

    // IsOwner isn't reliably set yet by Start() in all cases - checking it
    // in OnNetworkSpawn() instead, matching the pattern used elsewhere in
    // this project (ProjectileSpawner, MatchManager, etc.).
    public override void OnNetworkSpawn()
    {
        playerCamera.enabled = IsOwner;
        audioListener.enabled = IsOwner;
    }
}