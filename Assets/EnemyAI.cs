using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrolling, Chasing, Attacking, Returning, Dead }

    [Header("Setup")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask playerLayer;
    public float attackDistance = 2f;
    public float aggroDistance = 10f;
    public float lightAggroDistance = 15f;
    public float attackCooldown = 2f;

    [Header("Patrol")]
    public Transform patrolPoint;
    private Vector3 startPoint;

    [Header("Aggro")]
    public bool isLit = false;
    private float lastAttackTime;

    private EnemyState currentState;

    private void Start()
    {
        currentState = EnemyState.Patrolling;
        startPoint = patrolPoint.position;
    }

    private void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling:
                HandlePatrolling();
                break;

            case EnemyState.Chasing:
                HandleChasing();
                break;

            case EnemyState.Attacking:
                HandleAttacking();
                break;

            case EnemyState.Returning:
                HandleReturning();
                break;

            case EnemyState.Dead:
                // Do nothing
                break;
        }
    }

    // === PATROLLING STATE ===
    void HandlePatrolling()
    {
        agent.SetDestination(startPoint);

        if (CanSeePlayer())
        {
            currentState = EnemyState.Chasing;
        }
    }

    // === CHASING STATE ===
    void HandleChasing()
    {
        agent.SetDestination(player.position);

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackDistance)
        {
            currentState = EnemyState.Attacking;
        }
        else if (!CanSeePlayer())
        {
            currentState = EnemyState.Returning;
        }
    }

    // === ATTACKING STATE ===
    void HandleAttacking()
    {
        agent.SetDestination(transform.position); // Stop moving

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackDistance)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            AttackPlayer();
            lastAttackTime = Time.time;
        }
    }

    // === RETURNING STATE ===
    void HandleReturning()
    {
        agent.SetDestination(startPoint);

        if (Vector3.Distance(transform.position, startPoint) < 1f)
        {
            currentState = EnemyState.Patrolling;
        }
        else if (CanSeePlayer())
        {
            currentState = EnemyState.Chasing;
        }
    }

    // === DETECTION ===
    bool CanSeePlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // Base detection on light status or proximity
        if (isLit && distance <= lightAggroDistance) return true;
        if (!isLit && distance <= aggroDistance) return true;

        return false;
    }

    // === ATTACK ===
    void AttackPlayer()
    {
        //Debug.Log("Enemy attacked");
        // readd attack logic
    }

    // === STEALTH KILL SUPPORT ===
    public bool CanBeStealthKilled()
    {
        return currentState == EnemyState.Patrolling && !isLit;
    }

    public void StealthKill()
    {
        if (!CanBeStealthKilled()) return;

        //Debug.Log("Stealth killed!");
        currentState = EnemyState.Dead;
        agent.enabled = false;
        // Play death animation, spawn the thing
        Destroy(gameObject, 2f);
    }

    //Visual debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lightAggroDistance);
    }
}
