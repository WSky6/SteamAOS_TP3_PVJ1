using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Vida")]
    public float maxHealth = 50f;
    public int hitsToKill = 5;               // solo boss: golpes que aguanta

    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float acceleration = 30f;
    public float angularSpeed = 540f;
    public float turnSpeed = 180f;           // solo enemigo a distancia: qué tan rápido gira

    [Header("Salto")]
    public float maxJumpHeight = 1.8f;
    public float jumpDistance = 8f;
    public float jumpTriggerDistance = 1.5f;
    public float jumpSpeed = 9f;
    public float jumpArcHeight = 3.5f;
    public float jumpCooldown = 1.5f;

    [Header("Percepción")]
    public float visionRange = 12f;
    public float visionAngle = 120f;

    [Header("Mirar a los lados")]
    public float headScanAngle = 60f;
    public float headScanSpeed = 1f;

    [Header("Alerta a aliados")]
    public float allyAlertRadius = 10f;
    public float alertMemoryTime = 10f;      // a distancia: cuánto sigue alerta sin verte

    [Header("Ataque cuerpo a cuerpo")]
    public float attackRange = 1.8f;
    [Range(10f, 360f)] public float attackAngle = 90f;
    public bool instantKill = true;
    public float attackDamage = 10f;
    public float attackWindup = 0.5f;
    public float attackCooldown = 1.5f;

    [Header("Disparo")]
    public GameObject bulletPrefab;          // opcional
    public float bulletSpeed = 14f;
    public float bulletDamage = 25f;
    public float shootInterval = 3f;         // a distancia: 1 bala cada X segundos

    [Header("Ráfaga (boss)")]
    public int burstShots = 3;
    public float burstShotDelay = 0.25f;
    public float burstInterval = 4f;
    public float shootMinDistance = 8f;      // solo dispara si el player está más lejos que esto

    [Header("Recompensa")]
    public int points = 10;
}