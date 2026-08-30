using UnityEngine;

public class HealCard : Card
{
    [SerializeField] private Health playerHealth;
    void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<Health>();
    }

    public override void Play()
    {
        playerHealth.TakeDamage(-10);
    }
}