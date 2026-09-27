using UnityEngine;

/// <summary>
/// Marks NetworkManager's GameObject as persistent across scene loads, so
/// an active session survives the transition from the main menu into the
/// game scene. Functionally identical to PersistAcrossScenes - consider
/// consolidating to one shared script if both exist in the project.
/// </summary>
public class PersistNetworkManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}