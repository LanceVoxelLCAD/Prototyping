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
    public Renderer enemyRenderer;

    public float health = 30f;
    public float maxHealth = 30f;
    public float aggression;
    public float lastAggression;
    public float switchAttentionFromLightToPlayerDistance;
    public float enemySightMaxDistance = 30f;
    public float aggroSpeed = 15f;
    public float aggroDecay = 3f;
    public float aggroTrigger = 100f;
    public float maxAggro = 200f;
    public float minAggro = 0f;
    //public float bufferBeforeCalmingBegins = 8f;
    public bool isLit = false;
    public bool hasAggrod = false;
    public GameObject lastSeenLightProducer;
    public GameObject headObject;


    public float stoppingDistance;
    public float distanceToPlayer;
    public float distanceToLight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //find the goal (must be in root hierarchy)
        goal = GameObject.Find("Goal");
        originalGoal = goal;
        player = GameObject.Find("Player");
        agent = GetComponent<NavMeshAgent>();
        aggression = Random.Range(minAggro, aggroTrigger-1);
        //enemyRenderer = GetComponent<Renderer>();
        startMaterialColor = startMaterial.color;
        health = maxHealth;
        switchAttentionFromLightToPlayerDistance = stoppingDistance + 1f;
        lastSeenLightProducer = originalGoal;
    }

    // Update is called once per frame
    void Update()
    {
        IsLitTest();
        EnemyDebugingText();
        lastAggression = aggression;

        //convert to seconds

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

        if (aggression > aggroTrigger)
        {
            if (!hasAggrod && distanceToLight > switchAttentionFromLightToPlayerDistance)
            {
                goal = lastSeenLightProducer;
            }
            else if (!hasAggrod && distanceToPlayer < switchAttentionFromLightToPlayerDistance)
            {
                goal = player;
                hasAggrod = true;
            }
            //should switch in the little window, but stop if it comes too close
            //* added the hasAggrod since last functional, but trying out the above logic instead
            else if (hasAggrod && distanceToPlayer > stoppingDistance && distanceToPlayer < enemySightMaxDistance)
            {
                //i dont think this actually does anything here.. other than keep the max enemy sight line?
                goal = player;
                hasAggrod = true;
            }
            //*
            else if (distanceToLight <= stoppingDistance || distanceToPlayer <= stoppingDistance)
            {
                headObject.transform.LookAt(goal.transform); //this should only apply to the head, but this is fine for now
                agent.ResetPath(); //stop walking bro
            }
        }
        //previously: else if (aggression < aggroTrigger - bufferBeforeCalmingBegins)
        else if (aggression < aggroTrigger)
        {
            hasAggrod = false;
            goal = originalGoal;
        }
        else
        {
            goal = originalGoal;
        }

        //make aggressive if very close? might remove this later.
        if (distanceToPlayer < stoppingDistance)
        {
            aggression = aggroTrigger + 1f;
            goal = player;
            hasAggrod = true;
        }

        //if right up on a light, pay attention
        //should aggression rise faster if closer instead, perhaps? this is a hardcoded behavior
        //also doesnt trigger if theyve never seen a light now
        if (!hasAggrod && lastSeenLightProducer != originalGoal && distanceToLight < stoppingDistance)
        {
            aggression = aggroTrigger + 1f;
            goal = lastSeenLightProducer;
        }

        //decrease aggression when far away from player... or the light they saw.. and is not lit
        if (!isLit && (hasAggrod && distanceToPlayer > enemySightMaxDistance) || (!hasAggrod && distanceToLight > enemySightMaxDistance))
        {
            if (hasAggrod && aggression > minAggro) //could do the has aggro'd, or could save the random aggression generated as a minimum..
            {
                aggression -= aggroDecay * Time.deltaTime;
            }
            else if (aggression <= minAggro)
            {
                aggression = minAggro;
            }
        }
        //if still lit, but no longer by the player
        else if (isLit && (hasAggrod && distanceToPlayer > enemySightMaxDistance))
        {
            goal = lastSeenLightProducer;
            hasAggrod = false;
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
                if (aggression < maxAggro)
                {
                    aggression += aggroSpeed * Time.deltaTime; //increase one each second.. if it is hit by the light producer
                    //Debug.Log("We hit: " + hit.transform.gameObject.name + " at aggression level: " + aggression);
                }
                else if (aggression >= maxAggro)
                {
                    aggression = maxAggro;
                }

                //only updates when illuminated
                lastSeenLightProducer = hit.transform.gameObject;
                distanceToLight = Vector3.Distance(transform.position, lastSeenLightProducer.transform.position);
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

        aggression = maxAggro;
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
