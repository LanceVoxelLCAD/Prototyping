using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.Events;


public class EnemyController : MonoBehaviour
{
    [Header("Hookups")]
    public GameObject goal;
    private GameObject originalGoal;
    public GameObject player;
    private PlayerController playerController;

    [Header("Events")]
    public UnityEvent onDeath;

    [Header("Audio")]
    public EventReference ambientLoopEvent;
    public EventReference deathSound;
    private EventInstance ambientInstance;
    public EventReference freezeSound;


    public NavMeshAgent agent;
    public LayerMask mask;
    public Transform eyeline;

    //public TMP_Text healthNumDEBUG;
    public TMP_Text aggroNumDEBUG;
    public TMP_Text aggroTimeDEBUG;
    public float estimatedTimeToAggro;
    //public Slider healthSlider;
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
    private float distanceFactor;

    //public GameObject killedDummiesTrigger;

    [Header("Stats and Aggression")]
    public bool isPassive = false;
    //public float health = 30f;
    //public float maxHealth = 30f;
    public float maxSpeed;
    public float walkSpeed = 2f;
    public float attackDamage = 5f;
    public float aggression;
    public float lastAggression;
    public float switchAttentionFromLightToPlayerDistance;
    public float enemySightAngle = 60f;
    public float enemySightMaxDistance = 18f;
    public float enemySightCurr;
    public float enemySightCrouchBlindness = 10f;
    public float bumpedIntoDistance;
    public float stopDistFromPlayer = 3f;
    //public float aggroSpeed = 15f;
    private float baseAggroSpeed;
    public float aggroDecay = 3f;
    public float aggroTrigger = 100f;
    public float maxAggro = 200f;
    public float minAggro = 0f;
    public float attackDelay = 5f;
    public float attackDistance = 3.5f;
    //public float bufferBeforeCalmingBegins = 8f;
    public bool isLit = false;
    public bool hasAggrod = false;
    public bool hasStartedAttackCoroutine = false; //has aggrod
    public bool canAttack = false;
    public bool isAttacking = false;
    //public int blueCanisterRarity = 7;
    public int specialCanisterRarity = 8;
    public int ammoDropRarity = 6;
    public int healthDropRarity = 2;
    //public GameObject foodItem;

    public float distanceToPlayer;
    public float distanceToLight;
    public float attackWindupTime = .3f;

    [Header("Goo Slowdown")]

    public float maxGooAmount = 100f;
    public float gooDecayRate = 5f;
    public float gooPerHit = 35f;
    public float gooSneakDamageBoost = 2f;
    private float normalGooPerHitHolder;
    public float updateRate = .1f;

    public float gooAmount = 0f;
    public bool isFrozen = false;
    private Coroutine gooDecayCoroutineHolder;
    public float deadGlow = -.8f;
    public float chargePercentSetWhenFrozen = 85f;

    [Header("Beam Overcharge Logic")]
    public float maxBeamCharge = 100f;
    public float currentBeamCharge = 0f;
    public float beamChargeRate = 20f;
    //private bool EnemyUnaware;
    public float sneakBeamChargeBonus = 80f;
    private float normalBeamChargeRateHolder; //don't love this, but it is okay

    public float usualGlowItensity = -.1f;
    public float maxGlowIntensity = 5f;

    public float glowCurveExponent = 2.5f; //change in inspector

    public float glowDecayRate = .4f;
    //public float glowChargeSpeed = .8f; //visual glow only
    //public float glowCharge = 0f; //visual glow only
    public bool isBeingBeamed = false;
    float chargePercent = 0f;

    [Header("Patrol System")]
    public GameObject[] waypoints;
    public float waitTimeAtWaypoint = 4f;
    public int currWaypointIndex = 0;
    private Coroutine idleCoroutine;
    public bool isWaitingAtWaypoint = false;

    public enum EnemyState
    {
        Frozen,
        ChasePlayer,
        InvestigateLight,
        WanderSlashPatroling,
        PatrolIdle,
        Idle
    }

    public EnemyState currentState = EnemyState.Idle;

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
        if (!ambientLoopEvent.IsNull)
        {
            ambientInstance = RuntimeManager.CreateInstance(ambientLoopEvent);
            ambientInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            ambientInstance.start();
        }

        //find the goal (must be in root hierarchy)
        //goal = GameObject.Find("Goal");
        goal = gameObject; //stop looking for anything //allows for wandering
        originalGoal = goal;
        player = GameObject.Find("Player");
        playerController = player.GetComponent<PlayerController>();
        agent = GetComponent<NavMeshAgent>();
        //aggression = Random.Range(minAggro, aggroTrigger-1);
        //enemyRenderer = GetComponent<Renderer>();
        startMaterialColor = startMaterial.color;
        //health = maxHealth;
        maxSpeed = agent.speed;
        switchAttentionFromLightToPlayerDistance = attackDistance + 1f;
        lastSeenLightProducer = originalGoal;
        anim = GetComponent<Animator>();

        normalGooPerHitHolder = gooPerHit;
        normalBeamChargeRateHolder = beamChargeRate;

        //moved as this is repeated, this decides the state based on the number of waypoints
        SetOriginalState();

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
        anim.speed = Mathf.Lerp(1f, 0f, gooAmountPercent); //1 is goo-less, 0 is goo'd
            ambientInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));

        IsLitTest();
        EnemyDebugingText();
        lastAggression = aggression;

        if (isFrozen) return;

        UpdateGlowDecay();
        //UpdateGoalAndAggro();
        TryAttackWhenClose();

        HandleState();

        //convert to seconds
        //if (!isFrozen)
        //{



        //AGGRO LOGIC


        //if (isLit)
        //{
        //    enemyRenderer.material.color = Color.yellow;
        //}
        //else
        //{
        //    enemyRenderer.material.color = startMaterialColor;
        //}



        //}
       
    }

    private void SetState(EnemyState newState)
    {
        //i dont know if this is how this works ahhhhh!!!
        //this is basically "Start" for a state, I think
        if (currentState == newState) return;

        //cleanup - the example had multiple here, but I think I only need one....
        switch (currentState)
        {
            case EnemyState.PatrolIdle:
                if (idleCoroutine != null)
                {
                    //Debug.Log("idleCoroutine was stopped");
                    StopCoroutine(idleCoroutine);
                    idleCoroutine = null;
                }
                break;
        }

        currentState = newState;

        //setup for new state,handled when switch only.. i think
        switch (newState)
        {
            case EnemyState.Frozen:
                makeBeamGooNormal();
                agent.isStopped = true; //moved this here from update
                anim.speed = 0;
                break;

            case EnemyState.ChasePlayer:
                makeBeamGooNormal();
                agent.isStopped = false;
                agent.speed = maxSpeed;
                agent.stoppingDistance = stopDistFromPlayer;
                hasAggrod = true;
                goal = player;
                aggression = aggroTrigger + 1f;
                break;

            case EnemyState.InvestigateLight:
                makeBeamGooSneakyStrong();
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.stoppingDistance = stopDistFromPlayer;
                goal = lastSeenLightProducer;
                aggression = aggroTrigger + 2f;
                break;

            case EnemyState.WanderSlashPatroling:
                makeBeamGooSneakyStrong();
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.stoppingDistance = 0f;
                //currWaypointIndex = 0; //no, actually, should go back to the last point - 0 is in the inspector
                goal = waypoints[currWaypointIndex];
                //agent.SetDestination(waypoints[currWaypointIndex].transform.position);
                break;

            case EnemyState.PatrolIdle:
                makeBeamGooSneakyStrong();
                agent.isStopped = true;
                //anim.SetTrigger("SomeIdleAnimation,idk"); OR // anim.CrossFade("SomeIdle", 0.2f);
                idleCoroutine = StartCoroutine(IdleWaitAtWaypoint());
                break;

            case EnemyState.Idle:
                makeBeamGooSneakyStrong();
                agent.isStopped = true;
                agent.ResetPath();
                break;
        }
    }

    private void HandleState()
    {
        //and this is basically "Update" for a state, I think
        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        distanceToLight = lastSeenLightProducer ? Vector3.Distance(transform.position, lastSeenLightProducer.transform.position) : Mathf.Infinity;

        if (!isPassive) { CheckAggroConditions(); }

        switch (currentState)
        {
            case EnemyState.Frozen:
                return;

            case EnemyState.ChasePlayer:
                goal = player;
                agent.SetDestination(goal.transform.position);
                headObject.transform.LookAt(goal.transform);
                break;

            case EnemyState.InvestigateLight:
                if (lastSeenLightProducer)
                {
                    goal = lastSeenLightProducer;
                    agent.SetDestination(goal.transform.position);
                    headObject.transform.LookAt(goal.transform);
                }
                else
                {
                    SetOriginalState();
                }
                break;

            case EnemyState.WanderSlashPatroling:

                agent.SetDestination(goal.transform.position);

                //this line is internet code, unsure of it
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    //Debug.Log("Wandering is setting the state to patrol idle");
                    SetState(EnemyState.PatrolIdle);
                }
                break;

            case EnemyState.PatrolIdle:
                //can.. add something.. here... maybe. like an animation or something? random trigger?
                break;
            
            case EnemyState.Idle:
                break;

        }

        //if (ShouldFreeze())
        //{
        //    SetState(EnemyState.Frozen);
        //}
        //else if (ShouldChasePlayer())
        //{
        //    SetState(EnemyState.ChasePlayer);
        //}
        //else if (ShouldInvestigateLight())
        //{
        //    SetState(EnemyState.InvestigateLight);
        //}
        //else if (ShouldWander())
        //{
        //    SetState(EnemyState.Wander);
        //}
        //else
        //{
        //    SetState(EnemyState.Idle);
        //}
    }

    private void SetOriginalState()
    {
        if (waypoints.Length == 0)
        { //I only wanna do this once.. but then it never goes back to idle. hm. unsure of this
            waypoints = new GameObject[1];
            waypoints[0] = originalGoal;
        }

        if (waypoints.Length >= 1)
        {
            //SetState(EnemyState.WanderSlashPatroling); //if it is just one, it idles in place
            //okay maybe this will do better
            //Debug.Log("SetOriginalState is setting the state to patrol idle");
            SetState(EnemyState.PatrolIdle);
        }
        else
        {
            SetState(EnemyState.Idle);
        }
    }

    private bool ShouldFreeze()
    {
        return isFrozen;
    }

    private bool ShouldChasePlayer()
    {
        return goal == player;
    }

    private bool ShouldInvestigateLight()
    {
        return goal == lastSeenLightProducer;
    }
     
    private bool ShouldWander()
    {
        //idk this one yet fam
        return false;
    }

    private void makeBeamGooNormal()
    {
        //EnemyUnaware = false;
        gooPerHit = normalGooPerHitHolder;
        beamChargeRate = normalBeamChargeRateHolder;
    }

    private void makeBeamGooSneakyStrong()
    {
        //EnemyUnaware = true;
        gooPerHit *= gooSneakDamageBoost;
        beamChargeRate += sneakBeamChargeBonus; //should only call for the first hit, as the enemy will aggro

    }

    private IEnumerator IdleWaitAtWaypoint()
    {
        if (isWaitingAtWaypoint)
        {
            //well this didn't fix anything
            //Debug.Log("isWaiting prevented another coroutine");
            yield return null;
        }
        else
        {
            isWaitingAtWaypoint = true;

            yield return new WaitForSeconds(waitTimeAtWaypoint);

            //start traveling to next waypoint
            //stolen internet code....
            if (currentState == EnemyState.PatrolIdle) //as long as they're not chasing the player..
            { //apparently incrementing is wrong here for returning the old value before incrementing?
                currWaypointIndex = (currWaypointIndex + 1) % waypoints.Length;
                //agent.SetDestination(waypoints[currWaypointIndex].transform.position); //I think I'm saying this twice

                SetState(EnemyState.WanderSlashPatroling);
            }

            isWaitingAtWaypoint = false;
        }
    }

    private void UpdateGlowDecay()
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
    }

    private void CheckAggroConditions()
    {
        enemySightCurr = playerController.isCrouching ? enemySightCrouchBlindness : enemySightMaxDistance;

        //automatically aggro player if very close
        if (distanceToPlayer < bumpedIntoDistance)
        {
            if (!isPassive) { SetState(EnemyState.ChasePlayer); }
            return;
        }
        else if (distanceToPlayer < enemySightCurr)
        {
            // If player is within the vision cone, but not within bumping distance
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleToPlayer < enemySightAngle) //maybe 60f
            {
                //if within 15% of max sight. maybe make this an exposed inspector variable
                if (distanceToPlayer < enemySightCurr * .15f) //if right up on the enemy, instant aggro
                {
                    //Debug.LogError("The 10% sight thing got called.");
                    if (!isPassive) { SetState(EnemyState.ChasePlayer); }
                    return;
                }

                // player is within the vision cone
                // some stolen code modified for my purposes:

                // Step 1: Base aggro speed to reach full aggro in 5 seconds at max distance
                baseAggroSpeed = aggroTrigger / 5f; // e.x. 100 / 5 = 20

                // Step 2: Compute how close the player is (0 = far, 1 = close)
                float proximity = Mathf.InverseLerp(enemySightCurr, bumpedIntoDistance, distanceToPlayer);

                // Step 3: Scale the aggro speed - fast when close, slow when far
                float adjustedAggroSpeed = baseAggroSpeed * Mathf.Lerp(1f, 25f, proximity);

                aggression += adjustedAggroSpeed * Time.deltaTime;
                aggression = Mathf.Min(aggression, maxAggro);

                //DEBUG
                estimatedTimeToAggro = (aggroTrigger - aggression) / adjustedAggroSpeed;

                if (aggression >= aggroTrigger)
                {
                    if (!isPassive) { SetState(EnemyState.ChasePlayer); }
                    return;
                }
            }
        }

        //aggro light if very close and not already fixated on player
        if (!hasAggrod && lastSeenLightProducer != originalGoal && distanceToLight < attackDistance)
        {
            SetState(EnemyState.InvestigateLight);
            return;
        }

        // Main aggro switching logic
        if (aggression > aggroTrigger)
        {
            if (!hasAggrod)
            {
                if (distanceToPlayer < switchAttentionFromLightToPlayerDistance)
                {
                    if (!isPassive) { SetState(EnemyState.ChasePlayer); }
                }
                else if (lastSeenLightProducer != originalGoal && distanceToLight < switchAttentionFromLightToPlayerDistance)
                {
                    SetState(EnemyState.InvestigateLight);
                }
            }
            //else // already aggrod
            //{
            //    if (distanceToPlayer > stoppingDistance && distanceToPlayer < enemySightMaxDistance)
            //    {
            //        //continue chasing.. this might not be needed
            //        //code for when they're chasing the player, but they're still in visual range
            //        //goal = player;
            //        //Debug.LogError("Hey, you actually needed this! Turn it back on.");
            //    }
            //}

            // Keep looking at current goal
            //headObject.transform.LookAt(goal.transform);
        }
        else
        {
            //goal = originalGoal; //ugh if this breaks something go here....

            //moving hasAggro after so this only runs ONCE
            if (hasAggrod && currentState == EnemyState.ChasePlayer || currentState == EnemyState.InvestigateLight)
            {
                //Debug.Log("set state is called from checkaggroconditions");
                SetOriginalState();
            };

            // Calm down and reset goal
            hasAggrod = false;
        }

        // Decay aggro over time if out of range or not lit... also might not need this below if statement at all
        if (!isLit && (distanceToPlayer > enemySightCurr || (!hasAggrod && distanceToLight > enemySightCurr)))
        {
            aggression = Mathf.Max(aggression - aggroDecay * Time.deltaTime, minAggro);
        }
        // If lit but player is far, return to light
        else if (isLit && hasAggrod && distanceToPlayer > enemySightCurr / 4f)
        {
            goal = lastSeenLightProducer;
            hasAggrod = false;
            SetState(EnemyState.InvestigateLight);
        }





        /*
        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        agent.SetDestination(goal.transform.position);


        if (aggression > aggroTrigger)
        {
            if (!hasAggrod && distanceToLight > switchAttentionFromLightToPlayerDistance)
            {
                goal = lastSeenLightProducer;

                if (Vector3.Distance(transform.position, player.transform.position) < stoppingDistance)
                { 
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
        else
        {
            hasAggrod = false;
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
            hasAggrod = true;
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
        */
    }

    //private void AggroToPlayer(float forceAggro = -1f)
    //{
    //    hasAggrod = true;
    //    goal = player;
    //    if (forceAggro > 0f)
    //        aggression = Mathf.Max(aggression, forceAggro);
    //}

    //private void AggroToLight(float forceAggro = -1f) 
    //{
    //    goal = lastSeenLightProducer;
    //    if (forceAggro > 0f)
    //        aggression = Mathf.Max(aggression, forceAggro);
    //}


    private void TryAttackWhenClose()
    {
        bool closeToGoal =
            (goal == player && distanceToPlayer <= attackDistance ||
            goal != originalGoal && goal == lastSeenLightProducer && distanceToLight <= attackDistance);


        if (closeToGoal)
        {
            canAttack = true;
            if (!hasStartedAttackCoroutine)
            {
                attackCoroutine = StartCoroutine(AttackWhenClose());
            }
        }
        else
        {
            //no attacking, via a bool
            canAttack = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        {
            //draw line for how close enemy thinks player is
            Debug.DrawLine(transform.position, player.transform.position, Color.Lerp(Color.blue, Color.red, distanceFactor));

            //draw bumping distance (when enemy fully aggros)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, bumpedIntoDistance);

            ////draw instant aggro from sight (when enemy fully aggros & max sight)
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, enemySightCurr * .15f);
            Gizmos.DrawWireSphere(transform.position, enemySightCurr);

            //draw aggression decay range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackDistance);

            //draw light producer line if one was recently seen
            if (lastSeenLightProducer != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, lastSeenLightProducer.transform.position);
            }

            //draw line to player if currently aggro'd
            if (hasAggrod && player != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, player.transform.position);
            }

            //draw agent's destination if available
            if (goal != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, goal.transform.position);
                Gizmos.DrawSphere(goal.transform.position, 0.25f);
            }

            //this part is internet code, unsure about it:
            //vision cone settings
            Gizmos.color = Color.cyan;
            int coneSegments = 20;
            float angle = enemySightAngle; //half-angle of vision cone, so total FOV is 120° if 60°
            float radius = enemySightCurr;

            Vector3 forward = transform.forward;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-angle, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(angle, Vector3.up);

            Vector3 leftRay = leftRayRotation * forward * radius;
            Vector3 rightRay = rightRayRotation * forward * radius;

            Vector3 origin = transform.position;

            //draw left and right bounds
            Gizmos.DrawLine(origin, origin + leftRay);
            Gizmos.DrawLine(origin, origin + rightRay);

            //draw arc between the bounds
            Vector3 previousPoint = origin + leftRay;
            for (int i = 1; i <= coneSegments; i++)
            {
                float t = (float)i / coneSegments;
                float currentAngle = Mathf.Lerp(-angle, angle, t);
                Quaternion segmentRotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
                Vector3 currentPoint = origin + (segmentRotation * forward * radius);
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }
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
        //cleaned, in theory - but untested...
        if (isFrozen) return;

        if (other.CompareTag("LightCones"))
        {
            Transform lightProducer = other.transform.parent?.parent;

            if (!lightProducer) return;

            // Check line of sight to the light producer
            if (Physics.Linecast(eyeline.position, lightProducer.position, out RaycastHit hit, mask))
            {
                if (hit.transform.CompareTag("LightProducer"))
                {
                    // Increase aggression based on light exposure
                    aggression = Mathf.Min(aggression + baseAggroSpeed * Time.deltaTime, maxAggro);

                    // Update last seen light source and distance
                    lastSeenLightProducer = hit.transform.gameObject;
                    distanceToLight = Vector3.Distance(transform.position, lastSeenLightProducer.transform.position);
                }
            }
        }
        

        /*
        if (isFrozen) return;

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
        */
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

    //public void TakeDamage(float damageAmtReceived)
    //{
    //    if(isFrozen) { return; }

    //    health -= damageAmtReceived;

    //    if (health <= 0)
    //    {
    //        //if (player.TryGetComponent<PlayerController>(out PlayerController T))
    //        //{
    //        //    T.killcount++;
    //        //}

    //        DropItem(specialCanisterRarity, blueCanister);
    //        Die(blueCanister);
    //    }

    //    aggression = maxAggro;
    //}

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

        if (!isPassive) { SetState(EnemyState.ChasePlayer); }
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
        //if (isFrozen) { return; }

        gooAmount += gooPerHit;
        gooAmount = Mathf.Clamp(gooAmount, 0, maxGooAmount);
        UpdateGooMoveSpeed();

        if (gooDecayCoroutineHolder == null)
        {
            gooDecayCoroutineHolder = StartCoroutine(GooDecay());
        }

        if (!isPassive) { SetState(EnemyState.ChasePlayer); }
    }

    private void UpdateGooMoveSpeed()
    {

        float gooRatio = gooAmount / maxGooAmount;
        float newSpeed = maxSpeed * (1f - gooRatio);

        if (newSpeed <= .01f)
        {
            if (!freezeSound.IsNull)
            {
                RuntimeManager.PlayOneShot(freezeSound, transform.position); // Only plays ONCE now
            }

            //hopefully will stop the infinite frozen loot bug
            if (!isFrozen) { DropItem(specialCanisterRarity, yellowCanister); };
            isFrozen = true;
            gooDecayCoroutineHolder = null;
            Die(yellowCanister); //this could be a state or whatever..
        }

        agent.speed = newSpeed;
    }

    public void DropItem(int rarity, GameObject canister) //this could be combined with the die script...
    {
        Vector3 dropPos = transform.position + Vector3.up * .5f;

        int randomDrop = Random.Range(0, rarity + 1);

        //this could be way better.

        if (randomDrop < healthDropRarity) //if less than 2
        {
            Instantiate(redCanister, dropPos, transform.rotation);
        } 
        else if (randomDrop < ammoDropRarity) //if 2 or more, AND less than 4
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
            UpdateGooMoveSpeed();
        }

        gooDecayCoroutineHolder = null;
    }

    public void Die(GameObject canister)
    {
        //if (isPassive)
        //{
        //    killedDummiesTrigger.SetActive(true);
        //}
        onDeath?.Invoke();

        if (!deathSound.IsNull)
        {
            // Play at this position
            RuntimeManager.PlayOneShot(deathSound, transform.position);

            ambientInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            ambientInstance.release();
        }
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
                if (canAttack && goal == player && distanceToPlayer <= attackDistance)
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
        aggroTimeDEBUG.text = "Aggro Time: " + estimatedTimeToAggro.ToString();
        //healthNumDEBUG.text = "Health: " + health.ToString();
        //healthSlider.value = health;
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
