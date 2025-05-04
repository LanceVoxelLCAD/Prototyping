using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    [Header("Hookups")]
    public GameObject goal;
    private GameObject originalGoal;
    private GameObject player;

    public NavMeshAgent agent;
    public LayerMask mask;
    public Transform eyeline;

    public TMP_Text healthNumDEBUG;
    public TMP_Text aggroNumDEBUG;
    public Slider healthSlider;
    public Material startMaterial;
    public Color startMaterialColor;
    public Renderer enemyRenderer;

    public Animator enemyAttackAnimator;
    public GameObject redCanister;
    public GameObject blueCanister;
    public GameObject yellowCanister;

    public GameObject lastSeenLightProducer;
    public GameObject headObject;

    private Animator anim;
    private Material[] allMaterials;
    private Renderer[] renderers;
    public ParticleSystem deathParticle;

    private Coroutine attackCoroutine;

    [Header("Stats")]
    public float health = 30f;
    public float maxHealth = 30f;
    public float attackDamage = 5f;
    public float aggression;
    public float lastAggression;
    public float switchAttentionFromLightToPlayerDistance;
    public float enemySightMaxDistance = 30f;
    public float aggroSpeed = 15f;
    public float aggroDecay = 3f;
    public float aggroTrigger = 100f;
    public float maxAggro = 200f;
    public float minAggro = 0f;
    public float attackDelay = 5f;
    //public float bufferBeforeCalmingBegins = 8f;
    public bool isLit = false;
    public bool hasAggrod = false;
    public bool hasStartedAttackCoroutine = false; //has aggrod once
    public bool canAttack = false;
    public bool isAttacking = false;
    //public int blueCanisterRarity = 7;
    public int specialCanisterRarity = 8;
    public int yellowCanisterRarity = 6;
    public int redCanisterRarity = 2;
    //public GameObject foodItem;

    public float stoppingDistance;
    public float distanceToPlayer;
    public float distanceToLight;
    public float attackWindupTime = .3f;

    [Header("Goo Slowdown")]
    public float maxGooAmount = 100f;
    public float gooDecayRate = 5f;
    public float gooPerHit = 35f;
    public float updateRate = .1f;

    public float gooAmount = 0f;
    public float maxSpeed;
    public bool isFrozen = false;
    private Coroutine gooDecayCoroutineHolder;
    public float deadGlow = -.8f;
    public float chargePercentSetWhenFrozen = 85f;

    [Header("Beam Logic")]
    public float maxBeamCharge = 100f;
    public float currentBeamCharge = 0f;
    public float beamChargeRate = 20f;

    public float usualGlowItensity = -.1f;
    public float maxGlowIntensity = 5f;

    public float glowCurveExponent = 2.5f; //change in inspector

    public float glowDecayRate = .4f;
    //public float glowChargeSpeed = .8f; //visual glow only
    //public float glowCharge = 0f; //visual glow only
    public bool isBeingBeamed = false;
    float chargePercent = 0f;


    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        List<Material> mats = new List<Material>();

        foreach (var r in renderers)
        {
            mats.AddRange(r.materials); //agh im not completely confident in this
        }

        allMaterials = mats.ToArray();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //find the goal (must be in root hierarchy)
        //goal = GameObject.Find("Goal");
        goal = gameObject; //stop looking for anything //allows for wandering
        originalGoal = goal;
        player = GameObject.Find("Player");
        agent = GetComponent<NavMeshAgent>();
        aggression = Random.Range(minAggro, aggroTrigger-1);
        //enemyRenderer = GetComponent<Renderer>();
        startMaterialColor = startMaterial.color;
        health = maxHealth;
        maxSpeed = agent.speed;
        switchAttentionFromLightToPlayerDistance = stoppingDistance + 1f;
        lastSeenLightProducer = originalGoal;
        anim = GetComponent<Animator>();

        //attackCoroutine = StartCoroutine(AttackWhenClose());

        //should probably only start and play when first close to the player.. would help it not be all at once.
        //just kidding. i tried that and learned that kills unity
        //because im asking it to do something infinitely, an infinite number of times! cool
        //StartCoroutine(AttackWhenClose());
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetFloat("Speed", agent.velocity.magnitude);
        float targetSpeed = Mathf.Lerp(1f, 0f, gooAmountPercent); //1 is goo-less, 0 is goo'd
        anim.speed = targetSpeed;

        IsLitTest();
        EnemyDebugingText();
        lastAggression = aggression;



        //convert to seconds
        if (!isFrozen)
        {
            //glow decay
            if (!isBeingBeamed && currentBeamCharge > 0)
            {
                currentBeamCharge -= glowDecayRate * Time.deltaTime;
                currentBeamCharge = Mathf.Max(currentBeamCharge, 0f);
            }

            chargePercent = currentBeamCharge / maxBeamCharge;
            UpdateGlow(chargePercent);

            isBeingBeamed = false;


            //logic for glow independent of actual value
            //float target = isBeingBeamed ? 1f : 0f;

            //if (isBeingBeamed)
            //{
            //    //ease in
            //    glowCharge = Mathf.MoveTowards(glowCharge, target, glowChargeSpeed * Time.deltaTime);
            //}
            //else
            //{
            //    //linear decay
            //    glowCharge = Mathf.MoveTowards(glowCharge, target, glowDecayRate * Time.deltaTime);
            //}

            //UpdateGlow(glowCharge);
            //isBeingBeamed = false;


            //AGGRO LOGIC
            distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            //if (isLit)
            //{
            //    enemyRenderer.material.color = Color.yellow;
            //}
            //else
            //{
            //    enemyRenderer.material.color = startMaterialColor;
            //}

            agent.SetDestination(goal.transform.position);

            if (aggression > aggroTrigger)
            {
                if (!hasAggrod && distanceToLight > switchAttentionFromLightToPlayerDistance)
                {
                    goal = lastSeenLightProducer;
                    if (Vector3.Distance(lastSeenLightProducer.transform.position, player.transform.position) < stoppingDistance)
                    { //risky new code
                        goal = player;
                        hasAggrod = true;
                    }
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
                                                                 //agent.ResetPath(); //stop walking bro //this is unneeded, agents have a built in stopping distance?!!
                }
                headObject.transform.LookAt(goal.transform); //do it again.
            }
            //previously: else if (aggression < aggroTrigger - bufferBeforeCalmingBegins)
            else if (aggression < aggroTrigger)
            {
                hasAggrod = false;
                goal = originalGoal;
            }
            else //i dont think i need this either, it would only be called if equal.
            {
                goal = originalGoal;
            }

            //make aggressive if very close? might remove this later.
            if (distanceToPlayer < stoppingDistance)
            {
                if (aggression < aggroTrigger + 1f)
                {
                    aggression = aggroTrigger + 1f;
                }
                goal = player;
                hasAggrod = true; //this is the only time hasaggro'd is true...
            }

            //if right up on a light, pay attention
            //should aggression rise faster if closer instead, perhaps? this is a hardcoded behavior
            //also doesnt trigger if theyve never seen a light now
            if (!hasAggrod && lastSeenLightProducer != originalGoal && distanceToLight < stoppingDistance)
            {
                if (aggression < aggroTrigger + 2f) //different number for debugging
                {
                    aggression = aggroTrigger + 2f;
                }
                goal = lastSeenLightProducer;
            }

            //decrease aggression when far away from player... or the light they saw.. and is not lit
            if (!isLit && (distanceToPlayer > enemySightMaxDistance) || (!hasAggrod && distanceToLight > enemySightMaxDistance))
            {
                //removed from above & below line requirement to not be aggro'd
                if (aggression > minAggro) //could do the has aggro'd, or could save the random aggression generated as a minimum..
                {
                    aggression -= aggroDecay * Time.deltaTime;
                }
                else if (aggression <= minAggro)
                {
                    aggression = minAggro;
                }
            }
            //if still lit, but no longer by the player
            else if (isLit && (hasAggrod && distanceToPlayer > enemySightMaxDistance / 4))
            {
                goal = lastSeenLightProducer;
                hasAggrod = false;
            }

            if (goal == player && distanceToPlayer <= stoppingDistance ||
                goal != originalGoal && goal == lastSeenLightProducer && distanceToLight <= stoppingDistance)
            {
                //attack player
                //pass damage to player script
                //animation?
                canAttack = true;
                if (!hasStartedAttackCoroutine)
                {
                    attackCoroutine = StartCoroutine(AttackWhenClose());
                    hasStartedAttackCoroutine = true;
                }
            }
            else
            {
                //no attacking, via a bool. also stop it when the object is deleted
                canAttack = false;

                //if (hasStartedAttackCoroutine)
                //{
                //    StopCoroutine(attackCoroutine);
                //    hasStartedAttackCoroutine = false;
                //}
            }
        }
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemySightMaxDistance);
    }

    private float gooAmountPercent => gooAmount / maxGooAmount;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Light")
        {
            isLit = true;
        }
    }

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
            Die(blueCanister);
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
        if(isFrozen) { return; }

        health -= damageAmtReceived;

        if (health <= 0)
        {
            //if (player.TryGetComponent<PlayerController>(out PlayerController T))
            //{
            //    T.killcount++;
            //}

            DropItem(specialCanisterRarity, blueCanister);
            Die(blueCanister);
        }

        aggression = maxAggro;
    }

    //this is replacing health, maybe
    public void ApplyBeam(float deltaTime)
    {
        isBeingBeamed = true;

        if (currentBeamCharge >= maxBeamCharge) return;

        currentBeamCharge += beamChargeRate * deltaTime;
        currentBeamCharge = Mathf.Min(currentBeamCharge, maxBeamCharge); //just in case clamp

        chargePercent = currentBeamCharge / maxBeamCharge;
        //isBeingBeamed = true; //this was for using a seperate charge value just for the visuals

        //UpdateGlow(chargePercent);

        if (currentBeamCharge >= maxBeamCharge)
        {
            if(!isFrozen) { DropItem(specialCanisterRarity, blueCanister); }
            Die(blueCanister);
        }
    }

    private void UpdateGlow(float glowChargePercent) //was glow percent before when just visual
    {
        //float intensity = Mathf.Lerp(usualGlowItensity, maxGlowIntensity, chargePercent);
        //float eased = Mathf.SmoothStep(0f, 1f, chargePercent);
        float curved = Mathf.Pow(glowChargePercent, glowCurveExponent);
        float intensity = Mathf.Lerp(usualGlowItensity, maxGlowIntensity, curved);

        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                mat.SetFloat("_GlowIntensity", intensity);
            }
        }
    }

    public void ApplyGoo()
    {
        if (isFrozen) { return; }

        gooAmount += gooPerHit;
        gooAmount = Mathf.Clamp(gooAmount, 0, maxGooAmount);
        UpdateMoveSpeed();

        if (gooDecayCoroutineHolder == null)
        {
            gooDecayCoroutineHolder = StartCoroutine(GooDecay());
        }
    }

    private void UpdateMoveSpeed()
    {
        float gooRatio = gooAmount / maxGooAmount;
        float newSpeed = maxSpeed * (1f - gooRatio);

        if (newSpeed <= .01f)
        {
            newSpeed = 0f;
            isFrozen = true;
            gooDecayCoroutineHolder = null;

            DropItem(specialCanisterRarity, yellowCanister);
            Die(yellowCanister);
        }

        agent.speed = newSpeed;
    }

    public void DropItem(int rarity, GameObject canister) //this could be combined with the die script...
    {
        Vector3 dropPos = transform.position + Vector3.up * .5f;

        int randomDrop = Random.Range(0, rarity + 1);

        //this could be way better.

        if (randomDrop < redCanisterRarity) //if less than 2
        {
            Instantiate(redCanister, dropPos, transform.rotation);
        } 
        else if (randomDrop < yellowCanisterRarity) //if 2 or more, AND less than 4
        {
            Instantiate(yellowCanister, dropPos, transform.rotation);
        }
        else if (randomDrop == rarity) //for the yellow, this probably just prints a yellow. goo kills therefore print resources....
        {
            Instantiate(canister, dropPos, transform.rotation);
        }
    }

    private IEnumerator GooDecay()
    {
        while (gooAmount > 0)
        {
            yield return new WaitForSeconds(updateRate);

            if (isFrozen) { yield break; }

            gooAmount -= gooDecayRate * updateRate;
            gooAmount = Mathf.Max(gooAmount, 0f);
            UpdateMoveSpeed();
        }

        gooDecayCoroutineHolder = null;
    }

    public void Die(GameObject canister)
    {
        //turn off coroutine
        StopAllCoroutines();
        //if it was holding a resource, it would just.. deparent it? or actually spawn it?

        if (canister == blueCanister)
        {
            Vector3 dropPos = transform.position + Vector3.up * 1f;
            Instantiate(deathParticle, dropPos, transform.rotation);

            Destroy(gameObject);
        }

        if (canister == yellowCanister)
        {
            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    mat.SetFloat("_GlowIntensity", deadGlow);
                }
            }
            currentBeamCharge = chargePercentSetWhenFrozen;
            //gameObject.AddComponent<Health>();
        }
    }

    private IEnumerator AttackWhenClose()
    {
        hasStartedAttackCoroutine = true;

        while (canAttack)
        {
            if (!isAttacking)
            {
                isAttacking = true;
                anim.SetTrigger("PerformAttack");

                yield return new WaitForSeconds(attackWindupTime);

                //this should maybe be invoked with a delay matching the length of the attack animation
                if (canAttack && goal == player && distanceToPlayer <= stoppingDistance)
                {                    
                    if (player.TryGetComponent<PlayerController>(out PlayerController T))
                    {
                        T.TakeDamage(attackDamage);
                    }
                }

                yield return new WaitForSeconds(attackDelay); //cooldown between attacks
                isAttacking = false;
            }

            yield return null; //i aparently need this
        }

        //and this
        hasStartedAttackCoroutine = false;
    }

    public void EnemyDebugingText()
    {
        aggroNumDEBUG.text = "Aggro: " + aggression.ToString();
        //healthNumDEBUG.text = "Health: " + health.ToString();
        healthSlider.value = health;
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
