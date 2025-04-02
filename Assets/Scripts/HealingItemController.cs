using UnityEngine;

public class HealingItemController : MonoBehaviour
{
    public float healDamage = 25f;

    public void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            if (other.TryGetComponent<PlayerController>(out PlayerController T))
            {
                //T.TakeDamage(-1 * healDamage); //this plays the damage effect, let me seperate them
                T.HealFromDamage(healDamage);
            }

            Destroy(gameObject);
        }
    }
}
