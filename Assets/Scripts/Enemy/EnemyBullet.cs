using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    static readonly RaycastHit[] buf = new RaycastHit[8];

    private float speed = 14f;
    private float damage = 25f;
    private float radius = 0.15f;
    private float lifeLeft = 6f;
    private Transform shooter;
    private Color impactColor = Color.yellow;

    public static EnemyBullet Spawn(GameObject prefab, Vector3 position, Vector3 direction,
                                    float speed, float damage, Color color, Transform shooter)
    {
        GameObject go;

        if (prefab != null)
        {
            go = Instantiate(prefab, position, Quaternion.LookRotation(direction));
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "EnemyBullet";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.3f;

            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
        }

        go.transform.rotation = Quaternion.LookRotation(direction);

        EnemyBullet b = go.GetComponent<EnemyBullet>();
        if (b == null) b = go.AddComponent<EnemyBullet>();

        b.speed = speed;
        b.damage = damage;
        b.shooter = shooter;
        b.impactColor = color;
        return b;
    }

    void Update()
    {
        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = transform.forward;
        float step = speed * Time.deltaTime;

        int n = Physics.SphereCastNonAlloc(transform.position, radius, dir, buf, step,
                    ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < n; i++)
        {
            Transform t = buf[i].transform;

            if (shooter != null && (t == shooter || t.IsChildOf(shooter))) continue;

            // Otros enemigos: la bala los atraviesa
            if (!IsPlayer(t) && t.GetComponentInParent<IDamageable>() != null) continue;

            if (buf[i].distance < best)
            {
                best = buf[i].distance;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            RaycastHit h = buf[bestIndex];
            bool hitPlayer = IsPlayer(h.transform);

            if (hitPlayer)
            {
                // Un disparo enemigo le saca 1/3 de la vida al player (ver PlayerHealth)
                PlayerHealth ph = h.transform.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeShot();
                }
                else
                {
                    IDamageable d = h.transform.GetComponentInParent<IDamageable>();
                    if (d != null) d.TakeDamage(damage);
                    else Debug.LogWarning("La bala le pegó al player, pero no tiene PlayerHealth ni IDamageable");
                }
            }

            // Explosión de chispas en el punto de impacto
            Vector3 point = h.distance > 0f ? h.point : transform.position;
            Vector3 normal = h.normal.sqrMagnitude > 0.01f ? h.normal : -transform.forward;
            BulletImpact.Spawn(point, normal, hitPlayer ? new Color(1f, 0.2f, 0.2f) : impactColor,
                               hitPlayer ? 22 : 16);

            Destroy(gameObject); // pared o player
            return;
        }

        transform.position += dir * step;
    }

    bool IsPlayer(Transform t)
    {
        Transform p = EnemyUtils.Player;
        return p != null && (t == p || t.IsChildOf(p));
    }
}