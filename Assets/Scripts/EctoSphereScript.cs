using UnityEngine;

public class EctoSphereScript : MonoBehaviour
{
    public float health = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //how about:
    /* from player script..
     * if (hit.transform.TryGetComponent<Health>(out Health T))
                //    {
                //        T.TakeDamage(attackDamage);
                //    }
     * 
     */
}
