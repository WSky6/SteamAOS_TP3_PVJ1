using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    static readonly RaycastHit[] buf = new RaycastHit[8];

    private float speed;
    private float damage;
    private float lifeLeft = 4f;
    private Transform owner;
    private Color bulletColor;

    public static void Spawn(Vector3 position, Vector3 direction, float speed, float damage,
                             Color color, Transform owner)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "PlayerBullet";
        go.transform.position = position;
        go.transform.rotation = Quaternion.LookRotation(direction);
        go.transform.localScale = Vector3.one * 0.15f;
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().material.color = color;

        PlayerBullet b = go.AddComponent<PlayerBullet>();
        b.speed = speed;
        b.damage = damage;
        b.owner = owner;
        b.bulletColor = color;
    }

    void Update()
    {
        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float step = speed * Time.deltaTime;

        int n = Physics.SphereCastNonAlloc(transform.position, 0.1f, transform.forward, buf, step,
                    ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < n; i++)
        {
            Transform t = buf[i].transform;
            if (owner != null && t.IsChildOf(owner)) continue;

            if (buf[i].distance < best)
            {
                best = buf[i].distance;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            RaycastHit h = buf[bestIndex];

            IDamageable d = h.transform.GetComponentInParent<IDamageable>();
            if (d != null) d.TakeDamage(damage);

            // Explosión de chispas en el punto de impacto
            Vector3 point = h.distance > 0f ? h.point : transform.position;
            Vector3 normal = h.normal.sqrMagnitude > 0.01f ? h.normal : -transform.forward;
            Color c = d != null ? new Color(1f, 0.45f, 0.1f) : bulletColor;
            BulletImpact.Spawn(point, normal, c, d != null ? 22 : 16);

            Destroy(gameObject); // pared o enemigo
            return;
        }

        transform.position += transform.forward * step;
    }
}