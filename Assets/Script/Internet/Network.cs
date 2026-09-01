using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;

public class NetworkUI : MonoBehaviour
{
    public TMPro.TMP_InputField joinCodeInput;
    public TMPro.TMP_Text joinCodeDisplay;
    public GameObject connectUI;
    public GameObject hud;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void Host()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        joinCodeDisplay.text = "Code: " + joinCode;

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

        NetworkManager.Singleton.StartHost();
        connectUI.SetActive(false);
        hud.SetActive(true);
    }

    public async void Join()
    {
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInput.text);

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData,
            allocation.HostConnectionData);

        NetworkManager.Singleton.StartClient();
        connectUI.SetActive(false);
        hud.SetActive(true);
    }
}