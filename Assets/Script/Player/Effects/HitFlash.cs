using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Briefly flashes the player's model red when they take damage, visible
/// to anyone looking at them (including the attacker) - called from
/// Health.ApplyDamage on the server, broadcast to all clients.
/// </summary>
public class HitFlash : NetworkBehaviour
{
    public Renderer playerRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;

    private Color originalColor;

    void Awake()
    {
        if (playerRenderer == null) playerRenderer = GetComponentInChildren<Renderer>();
        if (playerRenderer != null && playerRenderer.material.HasProperty("_Color"))
            originalColor = playerRenderer.material.color;
    }

    public void TriggerFlash()
    {
        FlashClientRpc();
    }

    [ClientRpc]
    void FlashClientRpc()
    {
        // Restart the flash if it's already mid-flash from a very recent hit.
        StopAllCoroutines();
        StartCoroutine(DoFlash());
    }

    IEnumerator DoFlash()
    {
        if (playerRenderer == null || !playerRenderer.material.HasProperty("_Color")) yield break;

        playerRenderer.material.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        playerRenderer.material.color = originalColor;
    }
}