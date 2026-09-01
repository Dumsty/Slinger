using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class Health : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private Slider healthBar;
    private TMP_Text healthText;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);
    private Vector3 spawnPos;

    void Start()
    {
        spawnPos = transform.position;
        if (IsServer) currentHealth.Value = maxHealth;

        if (IsOwner)
        {
            healthBar = GameObject.Find("HealthBar").GetComponent<Slider>();
            healthText = GameObject.Find("HealthText").GetComponent<TMP_Text>();
            healthBar.maxValue = maxHealth;
        }
    }

    void Update()
    {
        if (IsServer && currentHealth.Value <= 0)
            Respawn();

        if (!IsOwner) return;
        if (healthBar != null) healthBar.value = currentHealth.Value;
        if (healthText != null) healthText.text = $"{currentHealth.Value}/{maxHealth}";
    }

    public void TakeDamage(float amount)
    {
        if (IsServer) currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);
        else TakeDamageServerRpc(amount);
    }

    [ServerRpc(RequireOwnership = false)]
    void TakeDamageServerRpc(float amount)
    {
        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);
    }

    void Respawn()
    {
        currentHealth.Value = maxHealth;
        TeleportClientRpc(spawnPos);
    }

    [ClientRpc]
    void TeleportClientRpc(Vector3 pos)
    {
        if (IsOwner) transform.position = pos;
    }
}