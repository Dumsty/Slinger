using UnityEngine;

/// <summary>
/// Marks the GameObject it's attached to as persistent across scene
/// loads. Used on NetworkManager (so a host's session survives the
/// transition from the main menu into the game scene) and on any other
/// object that needs to carry over the same way.
/// </summary>
public class PersistAcrossScenes : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}