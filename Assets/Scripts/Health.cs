using UnityEngine;

public class Health : MonoBehaviour
{
    public float health = 1f;
    public float maxHealth = 1f;
    public float minHealth = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //get annother script, called death on thing
    }

    // Update is called once per frame
    void Update()
    {
        if (health < minHealth)
        {
            
        }
    }
}
