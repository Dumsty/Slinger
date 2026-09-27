using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Fades a full-screen overlay to black when the local player is blinded
/// (see BlindBoltExplode), fading back out as the timer runs down.
/// </summary>
public class PlayerVision : NetworkBehaviour
{
    public Image blindOverlay;
    private float blindTimer;
    private float retryTimer;

    public static PlayerVision Local()
    {
        foreach (var pv in FindObjectsByType<PlayerVision>(FindObjectsSortMode.None))
            if (pv.IsOwner) return pv;
        return null;
    }

    void Update()
    {
        if (!IsOwner) return;

        // BlindOverlay lives in the HUD, which may not exist yet when this
        // spawns, so keep retrying until it's found.
        if (blindOverlay == null)
        {
            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0)
            {
                retryTimer = 0.5f;
                GameObject obj = GameObject.Find("BlindOverlay");
                if (obj != null) blindOverlay = obj.GetComponent<Image>();
            }
            return;
        }

        // Alpha directly tracks the remaining timer, so it fades out
        // smoothly as the blind effect wears off.
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