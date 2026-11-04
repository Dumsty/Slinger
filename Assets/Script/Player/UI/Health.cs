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

    // Owner-side: time until the next void report is allowed while still
    // below the kill line. A fall is reported once; the retry only matters
    // if the respawn teleport somehow never arrives.
    private float voidReportTimer;
    private const float VoidReportRetryDelay = 2f;

    // Server-side: when the server last teleported this player. A void
    // report arriving shortly after was sent before that teleport reached
    // the owner, so it's stale and ignored - one fall can't score twice.
    private float lastTeleportTime = float.NegativeInfinity;
    private const float StaleVoidReportWindow = 1f;

    void Start()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
    }

    void Update()
    {
        if (!IsOwner) return;

        CheckForVoidFall();

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

    // The owner simulates its own Rigidbody (the NetworkTransform is
    // owner-authoritative), so it's the first to know it has fallen off
    // the map. It reports the fall; the server decides what that means.
    void CheckForVoidFall()
    {
        MatchManager mm = MatchManager.Singleton;
        if (mm == null) return;

        if (transform.position.y > mm.voidKillY)
        {
            voidReportTimer = 0f;
            return;
        }

        voidReportTimer -= Time.deltaTime;
        if (voidReportTimer > 0f) return;
        voidReportTimer = VoidReportRetryDelay;

        if (IsServer) HandleVoidFall();
        else ReportVoidFallServerRpc();
    }

    [ServerRpc]
    void ReportVoidFallServerRpc()
    {
        HandleVoidFall();
    }

    void HandleVoidFall()
    {
        if (Time.time - lastTeleportTime < StaleVoidReportWindow) return;

        if (MatchManager.Singleton != null)
            MatchManager.Singleton.ReportVoidFall(OwnerClientId);
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
        lastTeleportTime = Time.time;
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