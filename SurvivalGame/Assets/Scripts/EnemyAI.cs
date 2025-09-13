using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Behavior")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float stoppingDistance = 2f;

    [Header("Wandering Behavior")]
    [SerializeField] private float idleTime = 2f;
    [SerializeField] private float minWanderDistance = 5f;
    [SerializeField] private float maxWanderDistance = 20f;

    private Transform player;
    private NavMeshAgent navMeshAgent;

    private enum EnemyState { Idle, Wandering, Chasing, Attacking }

    private EnemyState currentState;

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        navMeshAgent.stoppingDistance = stoppingDistance;

        currentState = EnemyState.Idle;
    }

    private void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
            currentState = EnemyState.Chasing;

        switch (currentState)
        {
            case EnemyState.Chasing:
                ChasePlayer();
                Debug.Log("Chasing Player");
                break;

            case EnemyState.Wandering:
                Wander();
                Debug.Log("Wandering");
                break;

            case EnemyState.Idle:
                Idle();
                Debug.Log("Idling");
                break;

            case EnemyState.Attacking:
                Attack();
                Debug.Log("Attacking");
                break;
        }
    }

    private void ChasePlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= detectionRange)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(player.position);
        }
        else
            currentState = EnemyState.Wandering;
    }

    private void Wander()
    {
        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            // SamplePosition eğer verilen pozisyonda NavMesh yoksa en yakın NavMesh pozisyonunu bulur
            if (NavMesh.SamplePosition(GetRandomDestination(), out NavMeshHit hit, maxWanderDistance, 1))
                navMeshAgent.SetDestination(hit.position);
        }
    }

    private void Idle()
    {
        // Idle logic here
    }

    private void Attack()
    {
        // Attack logic here
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
