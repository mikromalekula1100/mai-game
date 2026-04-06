using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack }

    [Header("Detection")]
    public float detectionRange = 20f;
    public float attackRange = 15f;
    public float fieldOfView = 120f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 25f;
    public float fireRate = 1f;
    public float aimInaccuracy = 2f;

    [Header("Patrol")]
    public float patrolRadius = 10f;
    public float patrolWaitTime = 2f;

    private NavMeshAgent agent;
    private Transform player;
    private State currentState = State.Patrol;
    private float nextFireTime;
    private float patrolTimer;
    private Vector3 spawnPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        spawnPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        SetPatrolDestination();
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Patrol:
                Patrol(distToPlayer);
                break;
            case State.Chase:
                Chase(distToPlayer);
                break;
            case State.Attack:
                Attack(distToPlayer);
                break;
        }
    }

    void Patrol(float distToPlayer)
    {
        if (CanSeePlayer(distToPlayer))
        {
            currentState = State.Chase;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaitTime)
            {
                SetPatrolDestination();
                patrolTimer = 0f;
            }
        }
    }

    void Chase(float distToPlayer)
    {
        agent.SetDestination(player.position);

        if (distToPlayer <= attackRange && HasLineOfSight())
        {
            currentState = State.Attack;
        }
        else if (distToPlayer > detectionRange * 1.5f)
        {
            currentState = State.Patrol;
            SetPatrolDestination();
        }
    }

    void Attack(float distToPlayer)
    {
        agent.SetDestination(transform.position); // Stop moving

        // Face player
        Vector3 lookDir = (player.position - transform.position);
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);

        // Shoot
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }

        if (distToPlayer > attackRange * 1.2f || !HasLineOfSight())
        {
            currentState = State.Chase;
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        // Aim at player center with some inaccuracy
        Vector3 targetPos = player.position + Vector3.up * 1f;
        Vector3 dir = (targetPos - firePoint.position).normalized;

        // Add spread
        dir += new Vector3(
            Random.Range(-aimInaccuracy, aimInaccuracy) * 0.01f,
            Random.Range(-aimInaccuracy, aimInaccuracy) * 0.01f,
            Random.Range(-aimInaccuracy, aimInaccuracy) * 0.01f
        );

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = dir.normalized * bulletSpeed;

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
            b.isPlayerBullet = false;
    }

    bool CanSeePlayer(float dist)
    {
        if (dist > detectionRange) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        return HasLineOfSight();
    }

    bool HasLineOfSight()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 dir = (player.position + Vector3.up * 1f - origin).normalized;
        float dist = Vector3.Distance(origin, player.position);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist))
        {
            return hit.transform.CompareTag("Player");
        }
        return true;
    }

    void SetPatrolDestination()
    {
        Vector3 randomPoint = spawnPosition + Random.insideUnitSphere * patrolRadius;
        randomPoint.y = spawnPosition.y;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
