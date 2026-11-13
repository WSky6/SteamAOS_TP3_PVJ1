using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private int shotsToDie = 3;
    [SerializeField] private string firstLevelScene = "Nivel1";

    [Header("Game over (lo maneja otro script)")]
    [SerializeField] private UnityEvent onDiedUnityEvent;

    private float health;
    private bool dead;

    public float Current => playerStats.currentHealth;
    public float Max => playerStats.maxHealth;
    public float Percent =>
    playerStats.maxHealth > 0f
    ? playerStats.currentHealth / playerStats.maxHealth
    : 0f;
    public bool IsDead => dead;

    public event System.Action<float, float> OnHealthChanged; // (vida actual, vida máxima)
    public event System.Action OnDied;

    void Awake()
    {
        bool isFirstLevel = SceneManager.GetActiveScene().name == firstLevelScene;
        bool wasDead = playerStats.currentHealth <= 0f;
        if (isFirstLevel || wasDead)
            playerStats.currentHealth = playerStats.maxHealth;
    }

    void Start()
    {
        Debug.Log($"Vida inicial: {playerStats.currentHealth}/{playerStats.maxHealth}");
        OnHealthChanged?.Invoke(playerStats.currentHealth, playerStats.maxHealth);
    }

    // Lo llaman las balas enemigas: saca 1/3 de la vida (shotsToDie disparos y muere)
    public void TakeShot()
    {
        TakeDamage(playerStats.maxHealth / Mathf.Max(1, shotsToDie) + 0.01f);
    }
    // Daño genérico (IDamageable): lo usan los golpes melee y las zonas de muerte
    public void TakeDamage(float amount)
    {
        if (dead) return;

        playerStats.currentHealth =
            Mathf.Max(0f, playerStats.currentHealth - amount);

        Debug.Log(
            $"Player recibió {amount} de daño. Vida: " +
            $"{playerStats.currentHealth}/{playerStats.maxHealth}"
        );

        OnHealthChanged?.Invoke(
            playerStats.currentHealth,
            playerStats.maxHealth
        );

        if (playerStats.currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (dead) return;

        playerStats.currentHealth =
            Mathf.Min(
                playerStats.maxHealth,
                playerStats.currentHealth + amount
            );

        OnHealthChanged?.Invoke(
            playerStats.currentHealth,
            playerStats.maxHealth
        );
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