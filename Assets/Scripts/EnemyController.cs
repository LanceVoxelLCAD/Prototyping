using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{

    public GameObject goal;
    private GameObject originalGoal;
    private GameObject player;

    public NavMeshAgent agent;
    public LayerMask mask;
    public Transform eyeline;

    public TMP_Text healthNumDEBUG;
    public TMP_Text aggroNumDEBUG;
    public Material startMaterial;
    public Color startMaterialColor;
    private Renderer enemyRenderer;

    public float health = 30f;
    public float maxHealth = 30f;
    public float aggression;
    public float lastAggression;
    public float aggroSpeed = 15f;
    public float aggroDecay = 3f;
    public float aggroMax = 100f;
    public float bufferBeforeCalmingBegins = 20f;
    public bool isLit = false;
    public bool hasAggrod = false;

    public float stoppingDistance;
    public float distanceToPlayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //find the goal (must be in root hierarchy)
        goal = GameObject.Find("Goal");
        originalGoal = goal;
        player = GameObject.Find("Player");
        agent = GetComponent<NavMeshAgent>();
        aggression = Random.Range(0, aggroMax-1);
        enemyRenderer = GetComponent<Renderer>();
        startMaterialColor = startMaterial.color;
        health = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        IsLitTest();
        EnemyDebugingText();
        lastAggression = aggression;

        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (isLit)
        {
            enemyRenderer.material.color = Color.yellow;
        }
        else
        {
            enemyRenderer.material.color = startMaterialColor;
        }

        agent.SetDestination(goal.transform.position);

        if (aggression > aggroMax)
        {
            if (distanceToPlayer > stoppingDistance)
            {
                goal = player;
            }
            else
            {
                transform.LookAt(player.transform); //this should only apply to the head, but this is fine for now
                agent.ResetPath(); //stop walking bro
            }
            hasAggrod = true;
        }
        else if (aggression < aggroMax - bufferBeforeCalmingBegins)
        {
            hasAggrod = false;
            goal = originalGoal;
        }
        else
        {
            goal = originalGoal;
        }

        if (hasAggrod) //could do the has aggro'd, or could save the random aggression generated as a minimum..
        {
            aggression -= aggroDecay * Time.deltaTime;
        }
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.tag == "Light")
    //    {
    //        isLit = true;
    //    }
    //}

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("collided with cone of light");
        //Debug.Log("seen cone of light w/ raycast");
        //if both are true:
        if(other.gameObject.tag == "LightCones")
        {
            //trigger is only active if the light is on, no need to get component on light to detect if on
            //if (ray from enemy face hits light origin)

            //if (!Physics.Linecast(transform.position, other.gameObject.transform.parent.parent.position))
            //...
            //RaycastHit hit;
            //if(Physics.Raycast(transform.position, transform.))
            //Debug.Log("Here is the parent's parent: " + other.gameObject.transform.parent.parent.name); //this is right
            //Debug.DrawLine(eyeline.position, other.gameObject.transform.parent.parent.position, Color.white, 3.5f);
            RaycastHit hit;
            Physics.Linecast(eyeline.position, other.gameObject.transform.parent.parent.position, out hit, mask);

            if (hit.transform.tag == "LightProducer")
            {
                //isLit = true;
                aggression += aggroSpeed * Time.deltaTime; //increase one each second.. if it is hit by the light producer
                //Debug.Log("We hit: " + hit.transform.gameObject.name + " at aggression level: " + aggression);
            } 
        }

        if (other.gameObject.name == "DeathCube")
        {
            //DEBUG
            Destroy(gameObject);
        }
    }

    private void IsLitTest()
    {
        if (lastAggression < aggression) //if aggression is rising
        {
            //isLit = true;
            if (isLit)
            {
                return; //don't run a million coroutines
            }
            else
            {
                StartCoroutine(SetIsLitTrueForAMoment()); //stops the flashing, maybe
            }
        }
        //else //if the same, or less...
        //{
        //    isLit = false;
        //}
    }

    private IEnumerator SetIsLitTrueForAMoment()
    {
        isLit = true;

        yield return new WaitForSeconds(.5f);

        isLit = false;
    }

    public void TakeDamage(float damageAmtReceived)
    {
        health -= damageAmtReceived;

        if (health <= 0)
        {
            Destroy(gameObject);
        }

        aggression = aggroMax;
    }

    public void EnemyDebugingText()
    {
        aggroNumDEBUG.text = "Aggro: " + aggression.ToString();
        healthNumDEBUG.text = "Health: " + health.ToString();
    }

    //private void OnTriggerExit(Collider other) //this doesn't fire when the flashlight is turned off
    //{
    //    if (other.gameObject.tag == "LightCones") 
    //    {
    //        isLit = false;
    //    }
    //}

    //private void OnTriggerExit(Collider other) //this DOES NOT WORK FOR TURNING OFF THE LIGHT INSIDE
    //{
    //    if (other.gameObject.tag == "Light")
    //    {
    //        isLit = false;
    //    }
    //}
}
