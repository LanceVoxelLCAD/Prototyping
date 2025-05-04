using UnityEngine;

public class Health : MonoBehaviour
{
    public float health = 1f;
    public float maxHealth = 16f;
    public float minHealth = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
    }

    public void TakeDamage(float damageAmtReceived)
    {
        health -= damageAmtReceived;

        if (health <= minHealth)
        {
            Destroy(gameObject);
            //Die();
            //get annother script, called death on thing
        }

    }
}
