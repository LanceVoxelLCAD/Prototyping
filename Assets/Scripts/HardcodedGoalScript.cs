using UnityEngine;

public class HardcodedGoalScript : MonoBehaviour
{
    public GameObject player;
    public float requiredKillcount = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (player.TryGetComponent<PlayerController>(out PlayerController T))
        {
            if(T.killcount >= requiredKillcount)
            {
                Destroy(gameObject);
            }
        }
    }
}
