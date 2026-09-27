using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Handles Unity Relay + Netcode setup for hosting and joining a match
/// from the main menu. Traffic is relayed through Unity's servers, so
/// neither player ever sees the other's IP address.
/// </summary>
public class NetworkUI : MonoBehaviour
{
    public TMPro.TMP_InputField joinCodeInput;
    public TMPro.TMP_Text joinCodeDisplay;
    public TMPro.TMP_Text errorDisplay;
    public GameObject connectUI;
    public GameObject duelUI;
    public string gameSceneName = "Game";

    // Stored here so it can be read later (e.g. displayed on the in-game
    // pause menu) after this object's own UI has been hidden.
    public static string HostJoinCode = "";

    async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void Host()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            HostJoinCode = joinCode;
            if (joinCodeDisplay != null) joinCodeDisplay.text = "Code: " + joinCode;

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();

            // Only the host/server triggers the scene load - Netcode's
            // scene manager pulls any connected clients along with it.
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            CloseMenus();
        }
        catch (System.Exception e)
        {
            ShowError("Failed to host: " + e.Message);
        }
    }

    public async void Join()
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInput.text);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData,
                allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
            // No scene load here - the host's LoadScene call above brings
            // this client along automatically once connected.
            CloseMenus();
        }
        catch (System.Exception e)
        {
            ShowError("Failed to join: " + e.Message);
        }
    }

    void CloseMenus()
    {
        if (connectUI != null) connectUI.SetActive(false);
        if (duelUI != null) duelUI.SetActive(false);
    }

    void ShowError(string message)
    {
        if (errorDisplay != null) errorDisplay.text = message;
        else Debug.LogError(message);
    }
}