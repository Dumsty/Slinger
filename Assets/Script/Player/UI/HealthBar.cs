using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;
    public float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.maxValue = maxHealth;
    }

    void Update()
    {
        healthBar.value = currentHealth;
        healthText.text = $"{currentHealth}/{maxHealth}";
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
    }
}