using UnityEngine;

public class PickupTrigger : MonoBehaviour
{
    private Pickup pickupParent;

    void Start()
    {
        pickupParent = GetComponentInParent<Pickup>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            pickupParent.ShowButtonPrompt(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            pickupParent.ShowButtonPrompt(false);
        }
    }
}
