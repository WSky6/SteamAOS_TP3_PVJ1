using System.Collections;
using UnityEngine;

// Palito del enemigo, a su derecha.
// Melee y boss: levantado (se mueve al atacar). A distancia: acostado (de la punta sale la bala).
public class EnemyWeapon : MonoBehaviour
{
    public enum Kind { Auto, MeleeRaised, RangedLying }

    [SerializeField] private Kind kind = Kind.Auto;
    [SerializeField] private Color stickColor = new Color(0.45f, 0.28f, 0.1f);

    public Transform Muzzle { get; private set; }

    private Transform pivot;
    private Coroutine swing;

    void Awake()
    {
        if (kind == Kind.Auto)
            kind = GetComponent<EnemyRanged>() != null ? Kind.RangedLying : Kind.MeleeRaised;

        Build();
    }

    void Build()
    {
        Transform old = transform.Find("Arma");
        if (old != null) Destroy(old.gameObject);

        pivot = new GameObject("Arma").transform;
        pivot.SetParent(transform, false);

        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stick.name = "Palito";
        Destroy(stick.GetComponent<Collider>());
        stick.transform.SetParent(pivot, false);
        stick.GetComponent<Renderer>().material.color = stickColor;

        if (kind == Kind.RangedLying)
        {
            // Acostado, apuntando hacia adelante, a la derecha
            pivot.localPosition = new Vector3(0.65f, 0.2f, 0.3f);
            stick.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            stick.transform.localScale = new Vector3(0.1f, 0.1f, 1f);

            Muzzle = new GameObject("Muzzle").transform;
            Muzzle.SetParent(pivot, false);
            Muzzle.localPosition = new Vector3(0f, 0f, 1f);
        }
        else
        {
            // Levantado, a la derecha
            pivot.localPosition = new Vector3(0.7f, 0f, 0.25f);
            stick.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            stick.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
        }
    }

    // Lo llama EnemyMelee / EnemyBoss con SendMessage al empezar el ataque
    void OnAttackStart(float windup)
    {
        if (kind != Kind.MeleeRaised || pivot == null) return;

        if (swing != null) StopCoroutine(swing);
        swing = StartCoroutine(Swing(windup));
    }

    IEnumerator Swing(float windup)
    {
        // 1) Se echa hacia atrás en la preparación
        float prep = Mathf.Max(0.05f, windup);
        float t = 0f;
        while (t < prep)
        {
            t += Time.deltaTime;
            pivot.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -30f, t / prep), 0f, 0f);
            yield return null;
        }

        // 2) Golpe rápido hacia adelante
        t = 0f;
        const float hit = 0.12f;
        while (t < hit)
        {
            t += Time.deltaTime;
            pivot.localRotation = Quaternion.Euler(Mathf.Lerp(-30f, 100f, t / hit), 0f, 0f);
            yield return null;
        }

        yield return new WaitForSeconds(0.15f);

        // 3) Vuelve a su posición
        t = 0f;
        const float back = 0.2f;
        while (t < back)
        {
            t += Time.deltaTime;
            pivot.localRotation = Quaternion.Euler(Mathf.Lerp(100f, 0f, t / back), 0f, 0f);
            yield return null;
        }

        pivot.localRotation = Quaternion.identity;
        swing = null;
    }
}