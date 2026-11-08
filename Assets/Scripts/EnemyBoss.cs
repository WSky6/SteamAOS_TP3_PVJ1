using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyJumper))]
public class EnemyBoss : MonoBehaviour, IDamageable
{
    private enum State { Idle, Chase, Attack, Dead }

    [SerializeField] private EnemyData data;

    [Header("Paredes / Visión")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float eyeOffset = 0.5f;

    [Header("Círculo de visión")]
    [SerializeField] private bool showVisionCircle = true;
    [SerializeField] private bool clipCircleByWalls = false;
    [SerializeField] private Color circleColor = new Color(1f, 0f, 0f, 0.12f);
    [SerializeField] private float circleYOffset = -0.95f;
    [SerializeField] private int circleSegments = 72;

    [Header("Visual")]
    [SerializeField] private Color bodyColor = new Color(0.35f, 0f, 0f);
    [SerializeField] private Color bulletColor = new Color(1f, 0.3f, 0f);

    private NavMeshAgent agent;
    private EnemyJumper jumper;
    private Transform player;
    private Renderer bodyRenderer;

    private GameObject circleObj;
    private Mesh circleMesh;
    private Vector3[] circleVerts;

    private State state = State.Idle;
    private int hits;
    private float nextAttackTime;
    private float nextBurstTime;
    private float nextRepath;
    private bool bursting;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        jumper = GetComponent<EnemyJumper>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Start()
    {
        agent.speed = data.moveSpeed;
        agent.acceleration = data.acceleration;
        agent.angularSpeed = data.angularSpeed;
        agent.stoppingDistance = data.attackRange * 0.8f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                agent.Warp(navHit.position);
            else
                Debug.LogWarning($"{name}: NO está sobre el NavMesh. Movelo sobre la zona celeste.");
        }

        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<Renderer>();
        if (bodyRenderer != null) bodyRenderer.material.color = bodyColor;

        EnemyUtils.CreateEye(transform);

        if (showVisionCircle)
        {
            circleObj = EnemyUtils.CreateFan(transform, "VisionCircle", circleYOffset, circleColor,
                                             circleSegments, out circleMesh, out circleVerts);
        }

        player = EnemyUtils.Player;
        if (player == null) Debug.LogWarning("EnemyBoss: no encuentro ningún objeto con el tag Player");

        jumper.Setup(data, player);
    }

    void Update()
    {
        if (state == State.Dead) return;

        if (player == null)
        {
            player = EnemyUtils.Player;
            if (player == null) return;
            jumper.Setup(data, player);
        }

        float dist = Vector3.Distance(transform.position, player.position);

        // Su "visión" es un círculo enorme: apenas el player entra, no lo suelta más
        if (state == State.Idle)
        {
            if (dist <= data.visionRange)
            {
                state = State.Chase;
                nextBurstTime = Time.time + 1.5f;
                if (circleObj != null) circleObj.SetActive(false);
            }
            return;
        }

        if (state != State.Chase) return;

        // Salto (obstáculos y atascos)
        jumper.Tick(dist);
        if (jumper.IsJumping) return;

        ChasePlayer();

        if (dist <= data.attackRange)
        {
            if (Time.time >= nextAttackTime) StartAttack();
        }
        else if (dist >= data.shootMinDistance && !bursting && Time.time >= nextBurstTime && HasLineOfSight())
        {
            StartCoroutine(Burst());
        }
    }

    void LateUpdate()
    {
        if (state != State.Idle || circleObj == null || !circleObj.activeSelf) return;

        LayerMask mask = clipCircleByWalls ? obstacleMask : (LayerMask)0;
        EnemyUtils.UpdateFan(transform, circleMesh, circleVerts, 360f, data.visionRange,
                             circleSegments, eyeOffset, mask);
    }

    // ---------------- MOVIMIENTO ----------------

    void ChasePlayer()
    {
        if (!agent.isOnNavMesh) return;

        if (Time.time < nextRepath) return;
        nextRepath = Time.time + 0.1f;

        Vector3 dest = player.position;
        if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            dest = hit.position;

        agent.SetDestination(dest);
    }

    // ---------------- DISPARO (ráfaga cuando está lejos) ----------------

    bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * eyeOffset;
        return !EnemyUtils.SegmentBlocked(origin, EnemyUtils.PlayerCenter(), transform, obstacleMask);
    }

    IEnumerator Burst()
    {
        bursting = true;
        nextBurstTime = Time.time + data.burstInterval;

        for (int i = 0; i < data.burstShots; i++)
        {
            if (state == State.Dead || player == null) break;

            FireBullet();
            yield return new WaitForSeconds(data.burstShotDelay);
        }

        bursting = false;
    }

    void FireBullet()
    {
        Vector3 flat = player.position - transform.position;
        flat.y = 0f;
        flat = flat.sqrMagnitude > 0.001f ? flat.normalized : transform.forward;

        Vector3 muzzle = transform.position + Vector3.up * eyeOffset + flat * transform.localScale.x;
        Vector3 dir = (EnemyUtils.PlayerCenter() - muzzle).normalized;

        EnemyBullet.Spawn(data.bulletPrefab, muzzle, dir, data.bulletSpeed, data.bulletDamage,
                          bulletColor, transform);
    }

    // ---------------- ATAQUE CUERPO A CUERPO ----------------

    void StartAttack()
    {
        state = State.Attack;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        nextAttackTime = Time.time + data.attackCooldown;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);

        // El palito hace el movimiento de golpe
        SendMessage("OnAttackStart", data.attackWindup, SendMessageOptions.DontRequireReceiver);

        Invoke(nameof(DealDamage), data.attackWindup);
        Invoke(nameof(EndAttack), data.attackWindup + 0.15f);
    }

    void DealDamage()
    {
        if (state == State.Dead || player == null) return;

        Vector3 to = player.position - transform.position;
        to.y = 0f;

        if (to.magnitude > data.attackRange * 1.3f) return;
        if (Vector3.Angle(transform.forward, to) > data.attackAngle * 0.5f) return;

        float dmg = data.instantKill ? 99999f : data.attackDamage;
        Debug.Log($"{name} golpea al player (daño {dmg})");

        IDamageable target = player.GetComponentInParent<IDamageable>();
        if (target != null) target.TakeDamage(dmg);
        else Debug.LogWarning("El player todavía no implementa IDamageable");
    }

    void EndAttack()
    {
        if (state == State.Dead) return;
        agent.isStopped = false;
        state = State.Chase;
    }

    // ---------------- VIDA (aguanta N golpes) ----------------

    public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;

        hits++;
        Debug.Log($"Boss recibió un golpe ({hits}/{data.hitsToKill})");
        StartCoroutine(Flash());

        if (hits >= data.hitsToKill) Die();
    }

    IEnumerator Flash()
    {
        if (bodyRenderer == null) yield break;

        bodyRenderer.material.color = Color.white;
        yield return new WaitForSeconds(0.12f);

        if (state != State.Dead) bodyRenderer.material.color = bodyColor;
    }

    void Die()
    {
        state = State.Dead;
        CancelInvoke();
        StopAllCoroutines();
        jumper.StopJumping();
        if (agent.enabled) agent.enabled = false;
        if (circleObj != null) circleObj.SetActive(false);

        // TODO: avisar al GameManager para sumar los puntos
        Debug.Log($"{name} murió (+{data.points} puntos)");
        Destroy(gameObject, 0.5f);
    }

    // Para probar sin script de disparo: clic derecho en el título del componente (en Play)
    [ContextMenu("Test: recibir golpe")]
    void TestHit() { TakeDamage(1f); }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.visionRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, data.shootMinDistance);
    }
}