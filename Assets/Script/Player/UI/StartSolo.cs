using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class SoloMode : MonoBehaviour
{
    public GameObject connectUI;
    public string gameSceneName = "Game";

    public void StartSolo()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777);
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnGameSceneLoaded;
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        if (connectUI != null) connectUI.SetActive(false);
    }

    void OnGameSceneLoaded(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        if (sceneName != gameSceneName) return;
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnGameSceneLoaded;
        MatchManager.Singleton?.EnterSoloMode();
    }
}