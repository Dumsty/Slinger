using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

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
        Debug.Log($"ApplyDamage called. MatchManager.Singleton={(MatchManager.Singleton != null ? "OK" : "NULL")}, " +
                $"phase={(MatchManager.Singleton != null ? MatchManager.Singleton.phase.Value.ToString() : "N/A")}, " +
                $"IsMatchInProgress={(MatchManager.Singleton != null && MatchManager.Singleton.IsMatchInProgress())}");

        if (MatchManager.Singleton == null || !MatchManager.Singleton.IsMatchInProgress()) return;
        if (currentHealth.Value <= 0) return;

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);

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

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.Teleport(pos);
        else transform.position = pos;
    }

    public void ClearBuffs()
    {
        ClearBuffsClientRpc();
    }

    [ClientRpc]
    void ClearBuffsClientRpc()
    {
        if (!IsOwner) return;

        BuffManager bm = BuffManager.Local();
        if (bm != null) bm.ClearAll();
    }
}