using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;

public class NetworkUI : MonoBehaviour
{
    public TMPro.TMP_InputField joinCodeInput;
    public TMPro.TMP_Text joinCodeDisplay;
    public TMPro.TMP_Text errorDisplay;
    public GameObject connectUI;
    public GameObject duelUI;
    public string gameSceneName = "Game";

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
            Debug.Log($"Join() called. joinCodeInput={(joinCodeInput != null ? "OK" : "NULL")}, " +
                    $"text='{(joinCodeInput != null ? joinCodeInput.text : "N/A")}'");

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInput.text);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData,
                allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
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