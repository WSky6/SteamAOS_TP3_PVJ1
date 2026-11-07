using UnityEngine;

public class EnemyRanged : MonoBehaviour
{
    private enum State { Idle, Alert, Dead }

    [SerializeField] private EnemyData data;

    [Header("Paredes / Visión")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float eyeOffset = 0.5f;

    [Header("Visual")]
    [SerializeField] private Color bodyColor = new Color(0.6f, 0f, 0.9f);
    [SerializeField] private Color bulletColor = new Color(1f, 0.9f, 0f);
    [SerializeField] private Color coneColor = new Color(0.7f, 0f, 1f, 0.22f);
    [SerializeField] private Color alertConeColor = new Color(1f, 0.55f, 0f, 0.30f);
    [SerializeField] private float coneYOffset = -0.95f;
    [SerializeField] private int coneSegments = 40;

    private Transform player;
    //private float health;
    private State state = State.Idle;

    private Quaternion baseRotation;
    private float scanPhase;
    private float nextShot;
    private float alertUntil;
    private Vector3 lastSeenPos;

    private GameObject coneObj;
    private Mesh coneMesh;
    private Vector3[] coneVerts;

    void Start()
    {
        //health = data.maxHealth;
        baseRotation = transform.rotation;
        scanPhase = Random.Range(0f, 6.28f);

        PaintBody();
        EnemyUtils.CreateEye(transform);
        coneObj = EnemyUtils.CreateFan(transform, "VisionCone", coneYOffset, coneColor,
                                       coneSegments, out coneMesh, out coneVerts);

        player = EnemyUtils.Player;
        if (player == null) Debug.LogWarning("EnemyRanged: no encuentro ningún objeto con el tag Player");
    }

    void Update()
    {
        if (state == State.Dead) return;

        if (player == null)
        {
            player = EnemyUtils.Player;
            if (player == null) return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Idle:
                ScanHead();
                if (CanSeePlayer(dist, true)) EnterAlert();
                break;

            case State.Alert:
                bool sees = CanSeePlayer(dist, false);

                if (sees)
                {
                    // Lo ve: lo sigue con la mirada y dispara
                    lastSeenPos = player.position;
                    alertUntil = Time.time + data.alertMemoryTime;
                    TurnTowards(lastSeenPos);

                    if (Time.time >= nextShot && AngleTo(lastSeenPos) < 20f)
                        Shoot();
                }
                else
                {
                    // No lo ve (pared en el medio): no dispara, sigue alerta
                    TurnTowards(lastSeenPos);

                    if (Time.time > alertUntil) LeaveAlert();
                }
                break;
        }
    }

    void LateUpdate()
    {
        if (state == State.Dead || coneObj == null) return;

        EnemyUtils.UpdateFan(transform, coneMesh, coneVerts, data.visionAngle, data.visionRange,
                             coneSegments, eyeOffset, obstacleMask);
    }

    // ---------------- ESTADOS ----------------

    void EnterAlert()
    {
        state = State.Alert;
        lastSeenPos = player.position;
        alertUntil = Time.time + data.alertMemoryTime;
        nextShot = Time.time + 1f; // pequeño tiempo de reacción
        EnemyUtils.SetFanColor(coneMesh, alertConeColor);
    }

    void LeaveAlert()
    {
        state = State.Idle;
        baseRotation = transform.rotation;
        EnemyUtils.SetFanColor(coneMesh, coneColor);
    }

    // ---------------- PERCEPCIÓN ----------------

    bool CanSeePlayer(float dist, bool useAngle)
    {
        if (dist > data.visionRange) return false;

        if (useAngle)
        {
            Vector3 flat = player.position - transform.position;
            flat.y = 0f;
            if (Vector3.Angle(transform.forward, flat) > data.visionAngle * 0.5f)
                return false;
        }

        Vector3 origin = transform.position + Vector3.up * eyeOffset;
        return !EnemyUtils.SegmentBlocked(origin, EnemyUtils.PlayerCenter(), transform, obstacleMask);
    }

    // ---------------- MOVIMIENTO DE CABEZA ----------------

    void ScanHead()
    {
        if (data.headScanAngle <= 0f) return;

        float angle = Mathf.Sin(Time.time * data.headScanSpeed + scanPhase) * data.headScanAngle;
        transform.rotation = baseRotation * Quaternion.Euler(0f, angle, 0f);
    }

    void TurnTowards(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, data.turnSpeed * Time.deltaTime);
    }

    float AngleTo(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return 0f;
        return Vector3.Angle(transform.forward, dir);
    }

    // ---------------- DISPARO ----------------

    void Shoot()
    {
        nextShot = Time.time + data.shootInterval;

        // La bala sale de la punta del palito acostado
        EnemyWeapon weapon = GetComponent<EnemyWeapon>();
        Vector3 muzzle = (weapon != null && weapon.Muzzle != null)
            ? weapon.Muzzle.position
            : transform.position + Vector3.up * eyeOffset + transform.forward * 0.8f;

        Vector3 dir = (EnemyUtils.PlayerCenter() - muzzle).normalized;

        EnemyBullet.Spawn(data.bulletPrefab, muzzle, dir, data.bulletSpeed, data.bulletDamage,
                          bulletColor, transform);
    }

    // ---------------- VISUAL ----------------

    void PaintBody()
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null) r = GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = bodyColor;
    }

    // ---------------- VIDA ----------------

    /*public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;

        health -= amount;
        if (health <= 0f) Die();
    }

    void Die()
    {
        state = State.Dead;
        if (coneObj != null) coneObj.SetActive(false);

        // TODO: avisar al GameManager para sumar los puntos
        Debug.Log($"{name} murió (+{data.points} puntos)");
        Destroy(gameObject, 0.5f);
    }*/

    // Para probar sin script de disparo: clic derecho en el título del componente (en Play)
    /*[ContextMenu("Test: recibir golpe")]
    void TestHit() { TakeDamage(data.maxHealth); }*/

    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.visionRange);

        Gizmos.color = Color.cyan;
        Vector3 l = Quaternion.Euler(0, -data.visionAngle * 0.5f, 0) * transform.forward;
        Vector3 r = Quaternion.Euler(0, data.visionAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, l * data.visionRange);
        Gizmos.DrawRay(transform.position, r * data.visionRange);
    }
}