using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Behavior")]
    [SerializeField] private float chaseRange = 15f;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float stoppingDistance = 2f;

    [Header("Wandering Behavior")]
    [SerializeField] private float idleTime = 2f;
    [SerializeField] private float minWanderDistance = 5f;
    [SerializeField] private float maxWanderDistance = 20f;

    private Transform player;
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    private bool playerinChaseRange = false;
    private bool playerinAttackRange = false;
    private bool alreadyAttacked = false;

    private float wanderTimer;
    private float timeBetweenAttacks = 1f;
    private float enemySpeed;

    private enum EnemyState { Idle, Wandering, Chasing, Attacking }

    private EnemyState currentState;

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        animator = GetComponent<Animator>();

        navMeshAgent.stoppingDistance = stoppingDistance;
        wanderTimer = idleTime;

        currentState = EnemyState.Wandering;
    }

    private void Update()
    {
        playerinChaseRange = Physics.CheckSphere(transform.position, chaseRange, LayerMask.GetMask("Player"));
        playerinAttackRange = Physics.CheckSphere(transform.position, attackRange, LayerMask.GetMask("Player"));

        if (!playerinChaseRange && !playerinAttackRange) currentState = EnemyState.Wandering;
        else if (playerinChaseRange && !playerinAttackRange) currentState = EnemyState.Chasing;
        else if (playerinChaseRange && playerinAttackRange) currentState = EnemyState.Attacking;

        switch (currentState)
        {
            case EnemyState.Wandering:
                Wander();
                Debug.Log("Wandering");
                break;

            case EnemyState.Chasing:
                ChasePlayer();
                Debug.Log("Chasing Player");
                break;

            case EnemyState.Attacking:
                Attack();
                Debug.Log("Attacking");
                break;

            case EnemyState.Idle:
                Idle();
                Debug.Log("Idling");
                break;
        }
    }

    private void Wander()
    {
        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            navMeshAgent.isStopped = true;
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = idleTime;
                navMeshAgent.isStopped = false;

                // SamplePosition eğer verilen pozisyonda NavMesh yoksa en yakın NavMesh pozisyonunu bulur
                if (NavMesh.SamplePosition(GetRandomDestination(), out NavMeshHit hit, maxWanderDistance, 1))
                    navMeshAgent.SetDestination(hit.position);

                enemySpeed = navMeshAgent.speed;
                animator.SetFloat("Speed", enemySpeed);
            }
        }
    }

    private void ChasePlayer()
    {
        navMeshAgent.isStopped = false;
        enemySpeed = navMeshAgent.speed;
        animator.SetFloat("Speed", enemySpeed);
        navMeshAgent.SetDestination(player.position);
    }
    private void Attack()
    {
        navMeshAgent.isStopped = true;
        Vector3 playerY = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(playerY);

        if (!alreadyAttacked)
        {
            animator.SetTrigger("Attack");

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    private void Idle()
    {
        // Idle logic here
    }

    private Vector3 GetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere.normalized;
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        randomDirection *= randomDistance;
        randomDirection += transform.position;
        return randomDirection;
    }

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
