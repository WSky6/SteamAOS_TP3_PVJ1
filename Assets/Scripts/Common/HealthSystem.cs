using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [Tooltip("Plantilla de donde sacamos la vida máxima")]
    [SerializeField] private EnemyStats enemyStats;
    [SerializeField] private GameObject vfx;

    // ¡Variable local! Cada enemigo tiene su propia vida independientemente de la plantilla
    private float currentHealth;
    public bool IsDead { get; private set; } = false;

    private Rigidbody rb;
    private Collider col;
    private MeshRenderer mesh;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        mesh = GetComponentInChildren<MeshRenderer>();

        if (enemyStats != null)
        {
            // Al nacer, copiamos la vida máxima de la plantilla a la variable local
            currentHealth = enemyStats.maxHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} recibió daño. Vida: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Dead();
        }
    }

    private void Dead()
    {
        IsDead = true;

        // Evaluamos quién murió gracias a los Tags del Paso 1
        if (gameObject.CompareTag("Enemy"))
        {
            GameManager2.Instance.NotifyEnemyDefeat(); // Enemigo normal: descuenta del contador
        }
        else if (gameObject.CompareTag("Boss"))
        {
            GameManager2.Instance.ChangeState(GameState.Victory); // Jefe: ¡Lanza la victoria directa!
        }

        if (vfx != null) vfx.SetActive(true);
        StartCoroutine(DestroyTransition());
    }

    IEnumerator DestroyTransition()
    {
        if (col != null) col.enabled = false;
        if (rb != null) rb.isKinematic = true;
        if (mesh != null) mesh.enabled = false;
        //
        // Apagamos el NavMeshAgent para que la IA no tire errores de movimiento al morir
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // Apagamos los scripts de movimiento y ataque (EnemyMelee, EnemyRange, etc.)
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s != this) s.enabled = false;
        }

        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }
}