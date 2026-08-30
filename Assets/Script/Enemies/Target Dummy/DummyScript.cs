using UnityEngine;
using TMPro;

public class Dummy : Enemy
{
    public GameObject damageTextPrefab;

    public override void TakeDamage(float amount)
    {
        GameObject text = Instantiate(damageTextPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
        text.GetComponent<TMP_Text>().text = amount.ToString();
        Destroy(text, 1f);

        base.TakeDamage(amount);
    }
}