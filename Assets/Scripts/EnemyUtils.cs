using UnityEngine;

// Funciones compartidas por el enemigo a distancia, el boss y el salto
public static class EnemyUtils
{
    static readonly RaycastHit[] buffer = new RaycastHit[16];
    static Transform player;
    static Collider playerCollider;

    public static Transform Player
    {
        get
        {
            if (player == null)
            {
                GameObject g = GameObject.FindGameObjectWithTag("Player");
                player = g != null ? g.transform : null;
            }
            return player;
        }
    }

    // Centro del cuerpo del player (hacia ahí apuntan las balas y la visión)
    public static Vector3 PlayerCenter()
    {
        Transform p = Player;
        if (p == null) return Vector3.zero;

        if (playerCollider == null) playerCollider = p.GetComponentInChildren<Collider>();
        return playerCollider != null ? playerCollider.bounds.center : p.position + Vector3.up;
    }

    // true = no cuenta como pared (uno mismo, el player y otros enemigos)
    public static bool IsIgnored(Transform hit, Transform self)
    {
        if (hit == self || hit.IsChildOf(self)) return true;

        Transform p = Player;
        if (p != null && (hit == p || hit.IsChildOf(p))) return true;

        if (hit.GetComponentInParent<IDamageable>() != null) return true;
        return false;
    }

    public static bool SegmentBlocked(Vector3 from, Vector3 to, Transform self, LayerMask mask)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.001f) return false;

        int count = Physics.RaycastNonAlloc(from, dir / dist, buffer, dist,
                        mask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (!IsIgnored(buffer[i].transform, self)) return true;
        }
        return false;
    }

    public static float WallDistance(Vector3 origin, Vector3 dir, float maxDist, Transform self, LayerMask mask)
    {
        int count = Physics.RaycastNonAlloc(origin, dir, buffer, maxDist,
                        mask, QueryTriggerInteraction.Ignore);
        float best = maxDist;

        for (int i = 0; i < count; i++)
        {
            if (IsIgnored(buffer[i].transform, self)) continue;
            if (buffer[i].distance < best) best = buffer[i].distance;
        }
        return best;
    }

    // Ojo blanco en el frente para ver hacia dónde mira
    public static void CreateEye(Transform parent)
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Ojo";
        Object.Destroy(eye.GetComponent<Collider>());
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = new Vector3(0f, 0.6f, 0.45f);
        eye.transform.localScale = Vector3.one * 0.3f;
        eye.GetComponent<Renderer>().material.color = Color.white;
    }

    public static GameObject CreateFan(Transform parent, string objName, float yOffset, Color color,
                                       int segments, out Mesh mesh, out Vector3[] verts)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent, false);
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

    public static void SetFanColor(Mesh mesh, Color c)
    {
        if (mesh == null) return;

        Color[] cols = new Color[mesh.vertexCount];
        for (int i = 0; i < cols.Length; i++) cols[i] = c;
        mesh.colors = cols;
    }

    // Abanico recortado por las paredes
    public static void UpdateFan(Transform t, Mesh mesh, Vector3[] verts, float angleSpan, float radius,
                                 int segments, float eyeOffset, LayerMask mask)
    {
        Vector3 eye = t.position + Vector3.up * eyeOffset;
        float half = angleSpan * 0.5f;
        float scale = Mathf.Max(0.0001f, t.lossyScale.x);

        verts[0] = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float a = Mathf.Lerp(-half, half, (float)i / segments);
            Vector3 localDir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;

            Vector3 worldDir = t.TransformDirection(localDir);
            worldDir.y = 0f;
            worldDir.Normalize();

            float d = WallDistance(eye, worldDir, radius, t, mask);
            verts[i + 1] = localDir * (d / scale);
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
    }
}