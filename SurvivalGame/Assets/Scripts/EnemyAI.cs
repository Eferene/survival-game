using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private float detectionRange = 15f; // Oyuncuyu algılama menzili
    [SerializeField] private float wanderRadius = 10f; // Rastgele dolaşma yarıçapı

    private enum State { Idle, Wandering, Chasing }

    private State currentState;

    private void Start()
    {
        currentState = State.Wandering;
    }

    private void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
            currentState = State.Chasing;
        else
            currentState = State.Wandering;

        if (currentState == State.Chasing)
            ChasePlayer();
        else if (currentState == State.Wandering)
            Wander();
    }

    private void ChasePlayer()
    {
        navMeshAgent.SetDestination(player.position);
    }

    private void Wander()
    {
        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, 1))
                navMeshAgent.SetDestination(hit.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
