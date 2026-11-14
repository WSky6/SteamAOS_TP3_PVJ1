using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyJumper : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float stuckCheckTime = 0.6f;
    [SerializeField] private float stuckMinMove = 0.25f;

    public bool IsJumping { get; private set; }

    private EnemyData data;
    private NavMeshAgent agent;
    private Transform player;
    private float nextJumpTime;
    private NavMeshPath testPath;
    private Vector3 stuckLastPos;
    private float stuckNextCheck;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void Setup(EnemyData enemyData, Transform target)
    {
        data = enemyData;
        player = target;
        ResetStuckTimer();
    }

    public void StopJumping()
    {
        StopAllCoroutines();
        IsJumping = false;
        enabled = false;
    }

    // Llamar cada frame mientras persigue
    public void Tick(float dist)
    {
        if (IsJumping || data == null || player == null) return;

        TryJump(dist);
        if (IsJumping) return;

        CheckStuck(dist);
    }

    // ---------------- ATASCO ----------------

    void CheckStuck(float dist)
    {
        if (!agent.isOnNavMesh || agent.isStopped)
        {
            ResetStuckTimer();
            return;
        }

        if (Time.time < stuckNextCheck) return;

        Vector3 moved = transform.position - stuckLastPos;
        moved.y = 0f;

        bool stuck = moved.magnitude < stuckMinMove && dist > data.attackRange * 1.5f;

        ResetStuckTimer();

        if (!stuck || Time.time < nextJumpTime) return;

        ForceJump();
    }

    void ResetStuckTimer()
    {
        stuckLastPos = transform.position;
        stuckNextCheck = Time.time + stuckCheckTime;
    }

    void ForceJump()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) return;
        toPlayer.Normalize();

        float[] angles = { 0f, 30f, -30f, 60f, -60f };

        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, angles[i], 0f) * toPlayer;
                if (TryJumpInDirection(dir, pass == 0)) return;
            }
        }
    }

    // ---------------- SALTO ----------------

    void TryJump(float dist)
    {
        if (Time.time < nextJumpTime || !agent.isOnNavMesh) return;
        if (dist <= data.attackRange * 1.5f) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();

        float footY = transform.position.y - agent.baseOffset;
        Vector3 lowOrigin = new Vector3(transform.position.x, footY + 0.3f, transform.position.z);
        Vector3 highOrigin = new Vector3(transform.position.x, footY + data.maxJumpHeight + 0.2f, transform.position.z);

        // Obstáculo bajo adelante...
        if (!ObstacleAhead(lowOrigin, dir, data.jumpTriggerDistance)) return;

        // ...pero no tan alto como una pared
        if (ObstacleAhead(highOrigin, dir, data.jumpTriggerDistance + 0.5f)) return;

        TryJumpInDirection(dir, true);
    }

    bool TryJumpInDirection(Vector3 dir, bool requirePathToPlayer)
    {
        float footY = transform.position.y - agent.baseOffset;
        if (testPath == null) testPath = new NavMeshPath();

        bool havePlayerNav = NavMesh.SamplePosition(player.position, out NavMeshHit playerNav, 3f, NavMesh.AllAreas);
        if (requirePathToPlayer && !havePlayerNav) return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distToPlayer = toPlayer.magnitude;

        float maxD = Mathf.Min(data.jumpDistance, distToPlayer - data.attackRange * 0.8f);
        if (maxD < 1.5f) return false;

        float[] factors = { 1f, 0.75f, 0.5f };
        Vector3 highOrigin = new Vector3(transform.position.x, footY + data.maxJumpHeight + 0.2f, transform.position.z);

        for (int i = 0; i < factors.Length; i++)
        {
            float d = maxD * factors[i];
            if (d < 1.5f) continue;

            Vector3 target = transform.position + dir * d;
            target.y = footY;

            if (!NavMesh.SamplePosition(target, out NavMeshHit nav, 1f, NavMesh.AllAreas)) continue;

            Vector3 flatMove = nav.position - transform.position;
            flatMove.y = 0f;
            if (flatMove.magnitude < d * 0.6f) continue;

            if (requirePathToPlayer)
            {
                if (!NavMesh.CalculatePath(nav.position, playerNav.position, NavMesh.AllAreas, testPath)) continue;
                if (testPath.status != NavMeshPathStatus.PathComplete) continue;
            }

            // Una pared en el camino
            if (ObstacleAhead(highOrigin, flatMove.normalized, flatMove.magnitude)) continue;

            // El arco no puede atravesar nada
            Vector3 startPos = transform.position;
            Vector3 endPos = nav.position + Vector3.up * agent.baseOffset;
            if (ArcBlocked(startPos, endPos)) continue;

            nextJumpTime = Time.time + data.jumpCooldown;
            StartCoroutine(JumpTo(nav.position));
            return true;
        }

        return false;
    }

    bool ArcBlocked(Vector3 start, Vector3 end)
    {
        const int steps = 12;
        Vector3 prev = start;

        for (int i = 1; i <= steps; i++)
        {
            float k = (float)i / steps;
            Vector3 p = Vector3.Lerp(start, end, k);
            p.y += Mathf.Sin(k * Mathf.PI) * data.jumpArcHeight;

            if (EnemyUtils.SegmentBlocked(prev, p, transform, obstacleMask)) return true;
            prev = p;
        }
        return false;
    }

    bool ObstacleAhead(Vector3 origin, Vector3 dir, float distance)
    {
        return EnemyUtils.SegmentBlocked(origin, origin + dir * distance, transform, obstacleMask);
    }

    IEnumerator JumpTo(Vector3 landing)
    {
        IsJumping = true;
        float offset = agent.baseOffset;
        agent.enabled = false;

        Vector3 start = transform.position;
        Vector3 end = landing + Vector3.up * offset;

        Vector3 flat = end - start;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat);

        float duration = Mathf.Max(0.45f, flat.magnitude / data.jumpSpeed);
        float t = 0f;
        bool crashed = false;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = Mathf.Clamp01(t);

            Vector3 p = Vector3.Lerp(start, end, k);
            p.y += Mathf.Sin(k * Mathf.PI) * data.jumpArcHeight;

            // Si choca con algo, corta el salto ahí
            if (EnemyUtils.SegmentBlocked(transform.position, p, transform, obstacleMask))
            {
                crashed = true;
                break;
            }

            transform.position = p;
            yield return null;
        }

        if (crashed)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                landing = hit.position;
            else
                landing = start - Vector3.up * offset;
        }

        transform.position = landing + Vector3.up * offset;
        agent.enabled = true;
        agent.Warp(landing);
        agent.ResetPath();
        ResetStuckTimer();
        IsJumping = false;
    }
}