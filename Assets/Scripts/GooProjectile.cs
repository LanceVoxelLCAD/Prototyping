using UnityEngine;

public class GooProjectile : MonoBehaviour
{
    public GameObject gooPlacedPrefab;
    public float lifetime = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //god this is so much cleaner than what I had before. bruh
        Destroy(gameObject, lifetime);
    }

    // Update is called once per frame
    void OnCollisionEnter(Collision collision)
    {
        //herm. I don't know about this. I think making it a trigger could be better...
        ContactPoint contact = collision.contacts[0];
        Quaternion gooRotation = Quaternion.LookRotation(contact.normal);

        if (collision.gameObject.tag != "GhostFoam")
        {
            //no infinite foam towers, sorry
            Instantiate(gooPlacedPrefab, contact.point, gooRotation);
        }

        Destroy(gameObject);
    }
}
