using UnityEngine;

public class FoodItemController : MonoBehaviour
{
    public float satiation = 45f;

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if (other.TryGetComponent<PlayerController>(out PlayerController T))
            {
                //T.TakeDamage(-1 * healDamage); //this plays the damage effect, let me seperate them
                //T.Eat(satiation);
            }

            Destroy(gameObject);
        }
    }
}
