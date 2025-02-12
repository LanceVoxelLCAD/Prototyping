using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{

    public GameObject goal;

    public NavMeshAgent agent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //find the goal (must be in root hierarchy)
        goal = GameObject.Find("Goal");
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        agent.SetDestination(goal.transform.position);
    }
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("collided with cone of light");
        //Debug.Log("seen cone of light w/ raycast");
        //if both are true:
        goal = GameObject.Find("Player");
    }
}
