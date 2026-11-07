using UnityEngine;
using UnityEngine.Events;

// ============================================================================
//  VIDA DEL PLAYER   <-- ACÁ SE MANEJA LA VIDA (para quien haga el HUD)
//
//  Leer desde el HUD:
//    PlayerHealth.Current   vida actual
//    PlayerHealth.Max       vida máxima
//    PlayerHealth.Percent   de 0 a 1 (para una barra)
//    PlayerHealth.IsDead
//  O suscribirse a los eventos:
//    OnHealthChanged(actual, maxima)   se dispara cada vez que cambia la vida
//    OnDied()                          se dispara UNA VEZ al morir
//
//  Reglas actuales:
//    - Una bala enemiga le saca 1/3 de la vida (aguanta 3 disparos): TakeShot()
//    - Un golpe melee lo mata de un golpe (TakeDamage con mucho daño)
//    - Caer en una zona DeathPlain lo mata (ver PlayerDeathZones)
//    - Para cambiar cuántos disparos aguanta: "Shots To Die" en el Inspector
//
//  GAME OVER:  <-- ACÁ SE ENGANCHA LA PANTALLA DE DERROTA
//  Este script NO muestra pantalla de derrota, no congela el juego ni reinicia nada.
//  Al morir solo apaga el arma y dispara el evento OnDied. Dos formas de engancharse:
//    a) Por código:       playerHealth.OnDied += MiMetodo;
//    b) Por Inspector:    el campo "On Died Unity Event" de abajo (arrastrar el objeto
//                         y elegir el método, sin escribir código)
//  Dentro de Die() está marcado el punto exacto para llamar al GameManager.
// ============================================================================
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int shotsToDie = 3;

    [Header("Game over (lo maneja otro script)")]
    [SerializeField] private UnityEvent onDiedUnityEvent;

    private float health;
    private bool dead;

    public float Current => health;
    public float Max => maxHealth;
    public float Percent => maxHealth > 0f ? health / maxHealth : 0f;
    public bool IsDead => dead;

    public event System.Action<float, float> OnHealthChanged; // (vida actual, vida máxima)
    public event System.Action OnDied;

    void Awake()
    {
        health = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(health, maxHealth);
    }

    // Lo llaman las balas enemigas: saca 1/3 de la vida (shotsToDie disparos y muere)
    public void TakeShot()
    {
        TakeDamage(maxHealth / Mathf.Max(1, shotsToDie) + 0.01f);
    }

    // Daño genérico (IDamageable): lo usan los golpes melee y las zonas de muerte
    public void TakeDamage(float amount)
    {
        if (dead) return;

        health = Mathf.Max(0f, health - amount);
        Debug.Log($"Player recibió {amount} de daño. Vida: {health}/{maxHealth}");

        OnHealthChanged?.Invoke(health, maxHealth);

        if (health <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (dead) return;

        health = Mathf.Min(maxHealth, health + amount);
        OnHealthChanged?.Invoke(health, maxHealth);
    }

    // ---------------- MUERTE ----------------

    void Die()
    {
        dead = true;
        Debug.Log("El player murió.");

        // Muerto no dispara
        PlayerWeapon weapon = GetComponent<PlayerWeapon>();
        if (weapon != null) weapon.enabled = false;

        // ===================== PUNTO DE ENGANCHE: GAME OVER =====================
        // Acá (o suscribiéndose a OnDied) va el game over:
        //   GameManager.Instance.GameOver();   <-- ejemplo, con el nombre real del compa
        // ========================================================================
        OnDied?.Invoke();
        onDiedUnityEvent?.Invoke();
    }
}