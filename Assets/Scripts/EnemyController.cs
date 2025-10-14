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
    [Tooltip("Shows the current enemy behavior state.")]
    public EnemyState currentState = EnemyState.Idle;

    [Header("Loot Drop Type")]
    [Tooltip("Determines what type of item this enemy drops when killed.")]
    public LootDropType lootDropType = LootDropType.None;

    [Header("Patrol System")]
    [Tooltip("Waypoints determine where the enemies will patrol.")]
    public GameObject[] waypoints;
    public float waitTimeAtWaypoint = 4f;
    public int currWaypointIndex = 0;
    private Coroutine idleCoroutine;
    public bool isWaitingAtWaypoint = false;

    [Header("Events")]
    public UnityEvent onDeath;

    [Header("Hookups")]
    public GameObject goal;
    private GameObject originalGoal;
    public GameObject player;
    private PlayerController playerController;
    public LayerMask playerRaycastMask;

    public NavMeshAgent agent;
    public LayerMask lightproducerRaycastMask;
    public Transform lightProducerEyeline;

    //public TMP_Text healthNumDEBUG;
    public TMP_Text aggroNumDEBUG;
    public TMP_Text aggroTimeDEBUG;
    public float estimatedTimeToAggro;
    //public Slider healthSlider;
    public Material startMaterial;
    public Color startMaterialColor;
    public Renderer enemyRenderer;

    public Animator enemyAttackAnimator;
    public GameObject healthDrop;
    public GameObject combatReward;
    public GameObject ammoDrop;

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
    public float enemySightHorizontalAngle = 60f;
    //public float enemySightVerticalAngle = 60f;
    public float enemySightVerticalAngleUp = 25f; //30f, maybe. make smaller, no one looks up
    public float enemySightVerticalAngleDown = 60f; //45, maybe.. make bigger. looking at feet, etc
    public float enemySightMaxDistance = 18f;
    public float enemySightCurr;
    public float enemySightCrouchBlindness = 10f;
    public float alertOtherEnemiesRadius = 10f;
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
    //public int specialCanisterRarity = 8;
    //public int ammoDropRarity = 6;
    //public int healthDropRarity = 2;
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

    [Header("Audio")]
    public EventReference ambientLoopEvent;
    public EventReference deathSound;
    private EventInstance ambientInstance;
    public EventReference freezeSound;

    public enum EnemyState
    {
        Frozen,
        ChasePlayer,
        InvestigateLight,
        WanderSlashPatroling,
        PatrolIdle,
        Idle
    }

    public enum LootDropType
    {
        None,
        HealthDrop,
        AmmoDrop,
        RandomHealthOrAmmo,
        CombatReward
    }

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
        TryAttackWhenClose();

        HandleState();
       
    }

    private void SetState(EnemyState newState)
    {
        //i dont know if this is how this works ahhhhh!!!
        //this is basically "Start" for a state, I think
        if (currentState == newState) return;

        //cleanup - the example had multiple here, but I think I only need one....
        //switch (currentState)
        //{
        //    case EnemyState.PatrolIdle:
        //        if (idleCoroutine != null)
        //        {
        //            Debug.Log("idleCoroutine was stopped at newState");
        //            StopCoroutine(idleCoroutine);
        //            idleCoroutine = null;
        //            isWaitingAtWaypoint = false;
        //        }
        //        break;
        //}

        //new cleanup
        if (idleCoroutine != null)
        {
            Debug.Log("idleCoroutine was stopped due to state change to " + newState);
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
            isWaitingAtWaypoint = false;
            agent.isStopped = false;
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
                AlertNearbyEnemies();
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
            //if player is within the vision cone, but not within bumping distance
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;

            //Line of Sight
            //there's already an eyeline gameobject and im not using it here
            //Vector3 eyePosition = transform.position + Vector3.up * 1.6f; //probably replace this with a var
            Vector3 eyePosition = lightProducerEyeline.transform.position;
            Vector3 targetPosition = playerController.transform.position + playerController.controller.center;

            float angleToPlayerHorizontal = Vector3.Angle(transform.forward, directionToPlayer);
            //float angleToPlayerVertical = Mathf.Abs(player.transform.position.y - transform.position.y); //different style... same, i think
            //float angleToPlayerVertical = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.right), directionToPlayer);
            Vector3 flatForward = Vector3.ProjectOnPlane(eyePosition, Vector3.right);
            float angleToPlayerVertical = Vector3.SignedAngle(flatForward, directionToPlayer, transform.right);
            //signed gives pos and neg so we can check up and down with it


            //worried about exiting the function too early actually
            //if (Physics.Raycast(eyePosition, (targetPosition - eyePosition).normalized, out RaycastHit hit, enemySightCurr))
            //{
            //    if (!hit.collider.CompareTag("Player"))
            //    {
            //        //something is blocking the view
            //        return; //abort sight-based aggro
            //    }
            //}
            //else
            //{
            //    //nothing hit
            //    return;
            //}

            if (Physics.Raycast(eyePosition, (targetPosition - eyePosition).normalized, out RaycastHit hit, enemySightCurr, playerRaycastMask, QueryTriggerInteraction.Ignore))
            {
                //Debug.Log("Looking at: " + hit.collider.gameObject.name);
                if (hit.collider.CompareTag("Player"))
                {
                    if (angleToPlayerHorizontal < enemySightHorizontalAngle &&
                        angleToPlayerVertical < enemySightVerticalAngleUp &&
                        angleToPlayerVertical > -enemySightVerticalAngleDown)
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

                        //base aggro speed to reach full aggro in 5 seconds at max distance
                        baseAggroSpeed = aggroTrigger / 5f; // e.x. 100 / 5 = 20

                        //find how close the player is (0 = far, 1 = close)
                        float proximity = Mathf.InverseLerp(enemySightCurr, bumpedIntoDistance, distanceToPlayer);

                        //scale aggro speed (fast when close, slow when far)
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
            }

           
        }

        //aggro light if very close and not already fixated on player
        if (!hasAggrod && lastSeenLightProducer != originalGoal && distanceToLight < attackDistance)
        {
            //Debug.Log("Investigating light at pos 1");
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
                    //Debug.Log("Investigating light at pos 2");
                    SetState(EnemyState.InvestigateLight);
                }
            }
        }
        else
        {
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
        else if (isLit && hasAggrod && distanceToPlayer > enemySightCurr)
        {
            goal = lastSeenLightProducer;
            hasAggrod = false;
            SetState(EnemyState.InvestigateLight);
            //Debug.Log("Investigating light at pos 3");
        }
    }

    //internet code interjection:
    private void AlertNearbyEnemies()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, alertOtherEnemiesRadius);

        foreach (Collider c in nearby)
        {
            EnemyController ally = c.GetComponent<EnemyController>();
            if (ally != null && ally != this)
            {
                //Debug.Log("Sending Alert from " + this.gameObject.name + " to " + ally.gameObject.name);
                ally.ReceiveAlert();
            }
        }
    }

    public void ReceiveAlert()
    {
        //alerts if not currently chasing
        if (currentState != EnemyState.ChasePlayer)
        {
            //goal = player;
            //aggression = aggroTrigger + 0.5f; //special aggro number for debuggin
            //so I don't lose my mcmarbles
            SetState(EnemyState.ChasePlayer);
        }
    }

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

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, alertOtherEnemiesRadius);

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
            //Gizmos.color = Color.cyan;
            //int coneSegments = 20;
            float angleHorizontal = enemySightHorizontalAngle; //half-angle of vision cone, so total FOV is 120° if 60°
            float radiusHorizontal = enemySightCurr;

            Vector3 forward = transform.forward;
            //Quaternion leftRayRotation = Quaternion.AngleAxis(-angleHorizontal, Vector3.up);
            //Quaternion rightRayRotation = Quaternion.AngleAxis(angleHorizontal, Vector3.up);

            //Vector3 leftRay = leftRayRotation * forward * radiusHorizontal;
            //Vector3 rightRay = rightRayRotation * forward * radiusHorizontal;

            Vector3 origin = lightProducerEyeline.transform.position;

            //draw left and right bounds
            //Gizmos.DrawLine(origin, origin + leftRay);
            //Gizmos.DrawLine(origin, origin + rightRay);

            ////draw arc between the bounds
            //Vector3 previousPoint = origin + leftRay;
            //for (int i = 1; i <= coneSegments; i++)
            //{
            //    float t = (float)i / coneSegments;
            //    float currentAngle = Mathf.Lerp(-angleHorizontal, angleHorizontal, t);
            //    Quaternion segmentRotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
            //    Vector3 currentPoint = origin + (segmentRotation * forward * radiusHorizontal);
            //    Gizmos.DrawLine(previousPoint, currentPoint);
            //    previousPoint = currentPoint;
            //}

            //visualize vertical sight limit
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            int horizontalSegments = 20;
            int verticalSegments = 10;

            for (int i = 0; i <= horizontalSegments; i++)
            {
                float horizontalAngle = Mathf.Lerp(-enemySightHorizontalAngle, enemySightHorizontalAngle, (float)i / horizontalSegments);
                Quaternion horizontalRot = Quaternion.AngleAxis(horizontalAngle, Vector3.up);

                for (int j = 0; j <= verticalSegments; j++)
                {
                    float verticalAngle = Mathf.Lerp(-enemySightVerticalAngleDown, enemySightVerticalAngleUp, (float)j / verticalSegments);
                    Quaternion verticalRot = Quaternion.AngleAxis(verticalAngle, Vector3.right);

                    Vector3 point = origin + verticalRot * horizontalRot * transform.forward * enemySightCurr;
                    Gizmos.DrawLine(origin, point);
                }
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

            //check line of sight to the light producer
            if (Physics.Linecast(lightProducerEyeline.position, lightProducer.position, out RaycastHit hit, lightproducerRaycastMask))
            {
                if (hit.transform.CompareTag("LightProducer"))
                {
                    //increase aggression based on light exposure
                    aggression = Mathf.Min(aggression + baseAggroSpeed * Time.deltaTime, maxAggro);

                    //update last seen light source and distance
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
            //if(!isFrozen) { DropItemRandom(specialCanisterRarity, combatDrop); }
            if(!isFrozen) { DropSelectedItem(); };
            Die(combatReward);
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
            //if (!isFrozen) { DropItemRandom(specialCanisterRarity, ammoDrop); };
            if (!isFrozen) { DropSelectedItem(); };
            isFrozen = true;
            gooDecayCoroutineHolder = null;
            Die(ammoDrop); //this could be a state or whatever..
        }

        agent.speed = newSpeed;
    }

    public void DropSelectedItem()
    {
        Vector3 dropPos = transform.position + Vector3.up * .5f;

        switch (lootDropType)
        {
            case LootDropType.None:
                return;

            case LootDropType.HealthDrop:
                Instantiate(healthDrop, dropPos, transform.rotation);
                return;

            case LootDropType.AmmoDrop:
                Instantiate(ammoDrop, dropPos, transform.rotation);
                return;

            case LootDropType.RandomHealthOrAmmo:
                int randomDrop = Random.Range(0, 2); //not inclusive in the upper bounds!!! barely remembered this
                if (randomDrop == 0)
                {
                    Instantiate(healthDrop, dropPos, transform.rotation);
                }
                else
                {
                    Instantiate(ammoDrop, dropPos, transform.rotation);
                }
                return;

            case LootDropType.CombatReward:
                Instantiate(combatReward, dropPos, transform.rotation);
                return;
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

        if (canister == combatReward)
        {
            Vector3 dropPos = transform.position + Vector3.up * 1f;
            Instantiate(deathParticle, dropPos, transform.rotation);

            Destroy(gameObject);
        }

        if (canister == ammoDrop)
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

}
