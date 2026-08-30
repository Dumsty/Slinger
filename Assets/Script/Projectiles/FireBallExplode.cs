using UnityEngine;

public class FireBallContact : MonoBehaviour
{
    public float damage = 10f;

    void OnCollisionEnter(Collision col)
    {
        Enemy enemy = col.gameObject.GetComponent<Enemy>();
        if (enemy != null) enemy.TakeDamage(damage);
        Destroy(gameObject);
    }
}