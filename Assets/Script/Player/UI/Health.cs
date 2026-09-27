using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Server-authoritative health: tracks currentHealth as a NetworkVariable,
/// applies damage only while a match is in progress, and reports kills to
/// MatchManager when health hits zero. Also exposes teleport/reset/buff
/// helpers that MatchManager calls between rounds.
/// </summary>
public class Health : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private Image healthBar;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);
    private float retryTimer;

    void Start()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
    }

    void Update()
    {
        if (!IsOwner) return;

        // HealthBar lives in the HUD, which may not exist yet when this
        // spawns, so keep retrying until it's found.
        if (healthBar == null)
        {
            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0)
            {
                retryTimer = 0.5f;
                GameObject obj = GameObject.Find("HealthBar");
                if (obj != null) healthBar = obj.GetComponent<Image>();
            }
            return;
        }

        healthBar.fillAmount = currentHealth.Value / maxHealth;
    }

    public void TakeDamage(float amount, ulong attackerId)
    {
        if (IsServer)
            ApplyDamage(amount, attackerId);
        else
            TakeDamageServerRpc(amount, attackerId);
    }

    [ServerRpc(RequireOwnership = false)]
    void TakeDamageServerRpc(float amount, ulong attackerId)
    {
        ApplyDamage(amount, attackerId);
    }

    void ApplyDamage(float amount, ulong attackerId)
    {
        // No damage (or healing, via a negative amount) outside an active
        // match/solo session - e.g. the lobby is a safe zone.
        if (MatchManager.Singleton == null || !MatchManager.Singleton.IsMatchInProgress()) return;
        if (currentHealth.Value <= 0) return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);

        HitFlash flash = GetComponent<HitFlash>();
        if (flash != null) flash.TriggerFlash();

        if (currentHealth.Value <= 0)
            MatchManager.Singleton.ReportKill(attackerId, OwnerClientId);
    }

    public void ResetHealth()
    {
        currentHealth.Value = maxHealth;
    }

    public void TeleportTo(Vector3 pos)
    {
        TeleportClientRpc(pos);
    }

    [ClientRpc]
    void TeleportClientRpc(Vector3 pos)
    {
        if (!IsOwner) return;

        // Routed through PlayerMovement.Teleport (Rigidbody-based) rather
        // than setting transform.position directly - a transform-only
        // teleport gets silently undone by the next physics step.
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.Teleport(pos);
        else transform.position = pos;
    }

    public void ClearBuffs()
    {
        ClearBuffsClientRpc();
    }

    // BuffManager's active buffs only exist on the owning client, so the
    // server routes this through a ClientRpc rather than touching it directly.
    [ClientRpc]
    void ClearBuffsClientRpc()
    {
        if (!IsOwner) return;

        BuffManager bm = BuffManager.Local();
        if (bm != null) bm.ClearAll();
    }
}