using UnityEngine;

public class DamageCard : Card
{
    [SerializeField] private Health playerHealth;
    void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<Health>();
    }
    public override void Play()
    {
        Debug.Log("DamageCard played");
        playerHealth.TakeDamage(10);
    }
}