using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MeleeAttack : NetworkBehaviour
{
    public float range = 2f;
    public float damage = 15f;
    public float cooldown = 0.5f;
    public AudioClip meleeSound;
    public float volume = 1f;
    private AudioSource audioSource;
    private float cooldownTimer;
    private Slider cooldownBar;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (cooldownBar == null)
        {
            GameObject obj = GameObject.Find("MeleeCooldownBar");
            if (obj != null) cooldownBar = obj.GetComponent<Slider>();
        }

        if (PauseMenu.paused || DeckStation.editingDeck) return;

        if (cooldownTimer > 0)
            cooldownTimer = Mathf.Max(0, cooldownTimer - Time.deltaTime);

        if (cooldownBar != null)
        {
            cooldownBar.gameObject.SetActive(cooldownTimer > 0);
            cooldownBar.value = cooldownTimer / cooldown;
        }

        if (Input.GetKeyDown(KeyCode.F) && cooldownTimer <= 0)
        {
            cooldownTimer = cooldown;
            Attack();
        }
    }

    void Attack()
    {
        if (audioSource != null && meleeSound != null)
            audioSource.PlayOneShot(meleeSound, volume);

        Transform cam = Camera.main.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range))
        {
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(damage);

            Health health = hit.collider.GetComponent<Health>();
            if (health != null) health.TakeDamage(damage, OwnerClientId);
        }
    }
}