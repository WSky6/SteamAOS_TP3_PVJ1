using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMelee : MonoBehaviour, IDamageable
{
    public enum State { Idle, Chase, Attack, Dead }

    // Lista de todos los enemigos vivos (para avisarse entre ellos)
    static readonly List<EnemyMelee> allEnemies = new List<EnemyMelee>();
    static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    [SerializeField] private EnemyData data;

    [Header("Paredes / Visión")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float eyeOffset = 0.5f;

    [Header("Atasco (salta si se queda trabado)")]
    [SerializeField] private float stuckCheckTime = 0.6f; // cada cuánto revisa si se movió
    [SerializeField] private float stuckMinMove = 0.25f;  // si se movió menos que esto, está atascado

    [Header("Visual")]
    [SerializeField] private Color bodyColor = Color.red;
    [SerializeField] private Color coneColor = new Color(1f, 0f, 0f, 0.25f);
    [SerializeField] private Color alertColor = new Color(0.2f, 0.4f, 1f, 0.15f);
    [SerializeField] private float coneYOffset = -0.95f;
    [SerializeField] private int coneSegments = 30;
    [SerializeField] private int alertSegments = 48;
    [SerializeField] private bool showAlertAreaAlways = false;

    private NavMeshAgent agent;
    private Transform player;
    private float health;
    private float nextAttackTime;
    private State state = State.Idle;

    // Giro de cabeza
    private Quaternion baseRotation;
    private float scanPhase;

    private float nextRepath;

    // Salto
    private bool jumping;
    private float nextJumpTime;
    private NavMeshPath testPath;

    // Atasco
    private Vector3 stuckLastPos;
    private float stuckNextCheck;

    private GameObject coneObj, alertObj;
    private Mesh coneMesh, alertMesh;
    private Vector3[] coneVerts, alertVerts;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Si tiene Rigidbody, lo desactivamos para que no se caiga ni se vuelque
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void OnEnable()  { allEnemies.Add(this); }
    void OnDisable() { allEnemies.Remove(this); }

    void Start()
    {
        health = data.maxHealth;
        agent.speed = data.moveSpeed;
        agent.acceleration = data.acceleration;
        agent.angularSpeed = data.angularSpeed;
        agent.stoppingDistance = data.attackRange * 0.8f;

        // Evitar que se amontonen: cada uno tiene prioridad distinta
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);

        // Si quedó un poco fuera del NavMesh, lo acomoda sobre el más cercano
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                agent.Warp(navHit.position);
            else
                Debug.LogWarning($"{name}: NO está sobre el NavMesh. Movelo sobre la zona celeste.");
        }

        baseRotation = transform.rotation;
        scanPhase = Random.Range(0f, 6.28f); // para que no giren todos sincronizados

        RemoveOldVisuals();
        PaintBody();
        CreateEye();
        coneObj = CreateFan("VisionCone", coneYOffset, coneColor, coneSegments, out coneMesh, out coneVerts);
        alertObj = CreateFan("AlertArea", coneYOffset - 0.02f, alertColor, alertSegments, out alertMesh, out alertVerts);
        SetState(State.Idle);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else Debug.LogWarning("EnemyMelee: no encuentro ningún objeto con el tag Player");
    }

    void Update()
    {
        if (state == State.Dead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Idle:
                ScanHead();
                if (CanSeePlayer(dist))
                {
                    SetState(State.Chase);
                    AlertAllies();
                }
                break;

            case State.Chase:
                if (jumping) break;

                TryJump(dist);
                if (jumping) break;

                CheckStuck(dist);
                if (jumping) break;

                ChasePlayer(dist);
                if (dist <= data.attackRange && Time.time >= nextAttackTime)
                    StartAttack();
                break;
        }
    }

    void LateUpdate()
    {
        if (state == State.Dead || coneObj == null) return;

        if (coneObj.activeSelf)
            UpdateFan(coneMesh, coneVerts, data.visionAngle, data.visionRange, coneSegments);
        if (alertObj.activeSelf)
            UpdateFan(alertMesh, alertVerts, 360f, data.allyAlertRadius, alertSegments);
    }

    // ---------------- MOVIMIENTO ----------------

    // Gira de un lado a otro; el cono y la detección siguen hacia dónde mira
    void ScanHead()
    {
        if (data.headScanAngle <= 0f) return;

        float angle = Mathf.Sin(Time.time * data.headScanSpeed + scanPhase) * data.headScanAngle;
        transform.rotation = baseRotation * Quaternion.Euler(0f, angle, 0f);
    }

    void ChasePlayer(float dist)
    {
        if (!agent.isOnNavMesh) return;

        // Actualiza el destino cada 0.1 s para evitar temblores
        if (Time.time < nextRepath) return;
        nextRepath = Time.time + 0.1f;

        Vector3 dest = player.position;

        // Solo se separa un poco si hay otro enemigo pegado y todavía está lejos del player
        if (dist > data.attackRange * 3f)
            dest += GetLightSeparation();

        if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            dest = hit.position;
        else
            dest = player.position;

        agent.SetDestination(dest);
    }

    // Empuje suave: solo cuenta a los enemigos que están a menos de 1.5 m
    Vector3 GetLightSeparation()
    {
        const float closeDistance = 1.5f;
        Vector3 push = Vector3.zero;

        for (int i = 0; i < allEnemies.Count; i++)
        {
            EnemyMelee other = allEnemies[i];
            if (other == this || other.state != State.Chase) continue;

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float d = away.magnitude;
            if (d > closeDistance || d < 0.01f) continue;

            push += away.normalized * (1f - d / closeDistance);
        }

        return Vector3.ClampMagnitude(push, 1f) * 1.5f;
    }

    // ---------------- ATASCO ----------------

    // Si en stuckCheckTime segundos casi no se movió y está lejos del player, salta
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

    // Salta hacia el player sin necesitar un obstáculo adelante
    void ForceJump()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) return;
        toPlayer.Normalize();

        float[] angles = { 0f, 30f, -30f, 60f, -60f };

        // Primero busca un aterrizaje conectado con el player; si no, uno cualquiera
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

    // Salto normal: hay un obstáculo bajo adelante
    void TryJump(float dist)
    {
        if (Time.time < nextJumpTime || !agent.isOnNavMesh) return;
        if (dist <= data.attackRange * 1.5f) return; // cerca del player no salta

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();

        float footY = transform.position.y - agent.baseOffset;
        Vector3 lowOrigin = new Vector3(transform.position.x, footY + 0.3f, transform.position.z);
        Vector3 highOrigin = new Vector3(transform.position.x, footY + data.maxJumpHeight + 0.2f, transform.position.z);

        // Tiene que haber un obstáculo bajo adelante...
        if (!ObstacleAhead(lowOrigin, dir, data.jumpTriggerDistance)) return;

        // ...pero no tan alto que no se pueda saltar (una pared)
        if (ObstacleAhead(highOrigin, dir, data.jumpTriggerDistance + 0.5f)) return;

        TryJumpInDirection(dir, true);
    }

    // Busca un aterrizaje lo más cerca posible del player en esa dirección y salta.
    // Devuelve true si saltó.
    bool TryJumpInDirection(Vector3 dir, bool requirePathToPlayer)
    {
        float footY = transform.position.y - agent.baseOffset;
        if (testPath == null) testPath = new NavMeshPath();

        bool havePlayerNav = NavMesh.SamplePosition(player.position, out NavMeshHit playerNav, 3f, NavMesh.AllAreas);
        if (requirePathToPlayer && !havePlayerNav) return false;

        // Distancia horizontal hasta el player
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distToPlayer = toPlayer.magnitude;

        // Apunta a caer justo antes del player, sin pasarse del alcance del salto
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

            // El aterrizaje tiene que estar conectado con el player
            if (requirePathToPlayer)
            {
                if (!NavMesh.CalculatePath(nav.position, playerNav.position, NavMesh.AllAreas, testPath)) continue;
                if (testPath.status != NavMeshPathStatus.PathComplete) continue;
            }

            // Pared: algo más alto que lo que se puede saltar en el camino
            if (ObstacleAhead(highOrigin, flatMove.normalized, flatMove.magnitude)) continue;

            // El arco del salto no puede atravesar nada
            Vector3 startPos = transform.position;
            Vector3 endPos = nav.position + Vector3.up * agent.baseOffset;
            if (ArcBlocked(startPos, endPos)) continue;

            nextJumpTime = Time.time + data.jumpCooldown;
            StartCoroutine(JumpTo(nav.position));
            return true;
        }

        return false;
    }

    // Revisa tramo por tramo que el arco no choque con nada
    bool ArcBlocked(Vector3 start, Vector3 end)
    {
        const int steps = 12;
        Vector3 prev = start;

        for (int i = 1; i <= steps; i++)
        {
            float k = (float)i / steps;
            Vector3 p = Vector3.Lerp(start, end, k);
            p.y += Mathf.Sin(k * Mathf.PI) * data.jumpArcHeight;

            if (SegmentBlocked(prev, p)) return true;
            prev = p;
        }
        return false;
    }

    bool SegmentBlocked(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.001f) return false;

        int count = Physics.RaycastNonAlloc(from, dir / dist, hitBuffer, dist,
                        obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (!IsIgnored(hitBuffer[i].transform, null)) return true;
        }
        return false;
    }

    bool ObstacleAhead(Vector3 origin, Vector3 dir, float distance)
    {
        int count = Physics.RaycastNonAlloc(origin, dir, hitBuffer, distance,
                        obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (!IsIgnored(hitBuffer[i].transform, null)) return true;
        }
        return false;
    }

    IEnumerator JumpTo(Vector3 landing)
    {
        jumping = true;
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

            // Seguridad: si choca con algo, corta el salto ahí
            if (SegmentBlocked(transform.position, p))
            {
                crashed = true;
                break;
            }

            transform.position = p;
            yield return null;
        }

        if (crashed)
        {
            // Aterriza en el NavMesh más cercano al punto donde chocó
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                landing = hit.position;
            else
                landing = start - Vector3.up * offset;
        }

        transform.position = landing + Vector3.up * offset;
        agent.enabled = true;
        agent.Warp(landing);
        agent.ResetPath();
        nextRepath = 0f; // vuelve a perseguir de inmediato
        ResetStuckTimer();
        jumping = false;
    }

    // ---------------- ESTADOS ----------------

    void SetState(State newState)
    {
        state = newState;

        if (state == State.Chase) ResetStuckTimer();

        if (coneObj != null)  coneObj.SetActive(state == State.Idle);
        if (alertObj != null) alertObj.SetActive(state == State.Idle ||
                                  (showAlertAreaAlways && state != State.Dead));
    }

    // Lo llama otro enemigo que ya vio al player
    public void Alert()
    {
        if (state == State.Idle) SetState(State.Chase);
    }

    void AlertAllies()
    {
        Vector3 from = transform.position + Vector3.up * eyeOffset;

        for (int i = 0; i < allEnemies.Count; i++)
        {
            EnemyMelee other = allEnemies[i];
            if (other == this || other.state != State.Idle) continue;

            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d > data.allyAlertRadius) continue;

            Vector3 to = other.transform.position + Vector3.up * eyeOffset;
            if (BlockedByWall(from, to, other.transform)) continue; // hay una pared en el medio

            other.Alert();
        }
    }

    // ---------------- PERCEPCIÓN ----------------

    bool CanSeePlayer(float dist)
    {
        if (dist > data.visionRange) return false;

        // Si está muy cerca, lo detecta aunque esté de espaldas
        bool veryClose = dist <= data.attackRange * 2f;
        if (!veryClose)
        {
            Vector3 flat = player.position - transform.position;
            flat.y = 0f;
            if (Vector3.Angle(transform.forward, flat) > data.visionAngle * 0.5f)
                return false;
        }

        Vector3 origin = transform.position + Vector3.up * eyeOffset;
        Vector3 target = player.position + Vector3.up * 1f;
        return !BlockedByWall(origin, target, player);
    }

    // Devuelve true si el objeto debe ignorarse (no cuenta como pared)
    bool IsIgnored(Transform t, Transform target)
    {
        if (t == transform || t.IsChildOf(transform)) return true;
        if (target != null && (t == target || t.IsChildOf(target))) return true;
        if (player != null && (t == player || t.IsChildOf(player))) return true;
        if (t.GetComponentInParent<EnemyMelee>() != null) return true; // otros enemigos no bloquean
        return false;
    }

    bool BlockedByWall(Vector3 from, Vector3 to, Transform target)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.01f) return false;

        int count = Physics.RaycastNonAlloc(from, dir / dist, hitBuffer, dist,
                        obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (!IsIgnored(hitBuffer[i].transform, target)) return true;
        }
        return false;
    }

    // Distancia hasta la primera pared en una dirección
    float WallDistance(Vector3 origin, Vector3 dir, float maxDist)
    {
        int count = Physics.RaycastNonAlloc(origin, dir, hitBuffer, maxDist,
                        obstacleMask, QueryTriggerInteraction.Ignore);
        float best = maxDist;

        for (int i = 0; i < count; i++)
        {
            if (IsIgnored(hitBuffer[i].transform, null)) continue;
            if (hitBuffer[i].distance < best) best = hitBuffer[i].distance;
        }
        return best;
    }

    // ---------------- VISUAL ----------------

    void PaintBody()
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null) r = GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = bodyColor;
    }

    // Borra conos viejos que pudo copiar un duplicado
    void RemoveOldVisuals()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.name == "VisionCone" || c.name == "AlertArea" || c.name == "Ojo")
                Destroy(c.gameObject);
        }
    }

    // Ojo blanco en el frente: sirve para ver hacia dónde mira
    void CreateEye()
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Ojo";
        Destroy(eye.GetComponent<Collider>());
        eye.transform.SetParent(transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.6f, 0.45f);
        eye.transform.localScale = Vector3.one * 0.3f;
        eye.GetComponent<Renderer>().material.color = Color.white;
    }

    GameObject CreateFan(string objName, float yOffset, Color color, int segments,
                         out Mesh mesh, out Vector3[] verts)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, yOffset, 0f);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.material = new Material(Shader.Find("Sprites/Default"));

        verts = new Vector3[segments + 2];
        Color[] cols = new Color[segments + 2];
        for (int i = 0; i < cols.Length; i++) cols[i] = color;

        int[] tris = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        mesh = new Mesh();
        mesh.MarkDynamic();
        mesh.vertices = verts;
        mesh.colors = cols;
        mesh.triangles = tris;
        mf.mesh = mesh;

        return go;
    }

    // Dibuja un abanico recortado por las paredes
    void UpdateFan(Mesh mesh, Vector3[] verts, float angleSpan, float radius, int segments)
    {
        Vector3 eye = transform.position + Vector3.up * eyeOffset;
        float half = angleSpan * 0.5f;
        float scale = Mathf.Max(0.0001f, transform.lossyScale.x);

        verts[0] = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float a = Mathf.Lerp(-half, half, (float)i / segments);
            Vector3 localDir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;

            Vector3 worldDir = transform.TransformDirection(localDir);
            worldDir.y = 0f;
            worldDir.Normalize();

            float d = WallDistance(eye, worldDir, radius);
            verts[i + 1] = localDir * (d / scale);
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
    }

    // ---------------- ATAQUE ----------------

    void StartAttack()
    {
        SetState(State.Attack);
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        nextAttackTime = Time.time + data.attackCooldown;

        // Apunta al player una sola vez y después no gira: así se puede esquivar
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);

        // El palito hace el movimiento de golpe
        SendMessage("OnAttackStart", data.attackWindup, SendMessageOptions.DontRequireReceiver);

        Invoke(nameof(DealDamage), data.attackWindup);
        Invoke(nameof(EndAttack), data.attackWindup + 0.4f);
    }

    void DealDamage()
    {
        if (state == State.Dead || player == null) return;

        Vector3 to = player.position - transform.position;
        to.y = 0f;

        if (to.magnitude > data.attackRange * 1.3f) return;                          // fuera de rango
        if (Vector3.Angle(transform.forward, to) > data.attackAngle * 0.5f) return;  // fuera del arco

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
        SetState(State.Chase);
    }

    // ---------------- VIDA ----------------

    public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;

        health -= amount;
        if (health <= 0f) Die();
    }

    void Die()
    {
        SetState(State.Dead);
        CancelInvoke();
        StopAllCoroutines();
        if (agent.enabled) agent.enabled = false;

        // TODO: avisar al GameManager para sumar los puntos
        Debug.Log($"{name} murió (+{data.points} puntos)");
        Destroy(gameObject, 0.5f);
    }

    // ---------------- GIZMOS (se ven al seleccionar al enemigo) ----------------

    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        // Amarillo: rango de visión
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.visionRange);

        // Azul: radio de alerta a aliados
        Gizmos.color = new Color(0.2f, 0.4f, 1f);
        Gizmos.DrawWireSphere(transform.position, data.allyAlertRadius);

        // Celeste: bordes del cono de visión
        Gizmos.color = Color.cyan;
        Vector3 l = Quaternion.Euler(0, -data.visionAngle * 0.5f, 0) * transform.forward;
        Vector3 r = Quaternion.Euler(0, data.visionAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, l * data.visionRange);
        Gizmos.DrawRay(transform.position, r * data.visionRange);

        // Magenta: arco del golpe
        Gizmos.color = Color.magenta;
        Vector3 al = Quaternion.Euler(0, -data.attackAngle * 0.5f, 0) * transform.forward;
        Vector3 ar = Quaternion.Euler(0, data.attackAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, al * data.attackRange);
        Gizmos.DrawRay(transform.position, ar * data.attackRange);
    }
}