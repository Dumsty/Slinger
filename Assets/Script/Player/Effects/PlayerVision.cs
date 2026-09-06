using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerVision : NetworkBehaviour
{
    public Image blindOverlay;
    private float blindTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        if (blindOverlay == null)
        {
            GameObject obj = GameObject.Find("BlindOverlay");
            if (obj != null) blindOverlay = obj.GetComponent<Image>();
        }
    }

    public static PlayerVision Local()
    {
        foreach (var pv in FindObjectsByType<PlayerVision>(FindObjectsSortMode.None))
            if (pv.IsOwner) return pv;
        return null;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (blindOverlay == null) return;

        if (blindTimer > 0)
        {
            blindTimer -= Time.deltaTime;
            Color c = blindOverlay.color;
            c.a = Mathf.Clamp01(blindTimer);
            blindOverlay.color = c;
        }
        else if (blindOverlay.color.a > 0)
        {
            Color c = blindOverlay.color;
            c.a = 0;
            blindOverlay.color = c;
        }
    }

    [ClientRpc]
    public void ApplyBlindClientRpc(float duration, ClientRpcParams rpcParams = default)
    {
        blindTimer = duration;
    }
}