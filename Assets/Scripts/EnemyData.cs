using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Vida")]
    public float maxHealth = 50f;

    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float acceleration = 30f;
    public float angularSpeed = 540f;

    [Header("Separación entre enemigos")]
    public float separationDistance = 3f;   
    public float separationStrength = 2.5f; 

    [Header("Percepción")]
    public float visionRange = 12f;
    public float visionAngle = 120f;

    [Header("Mirar a los lados")]
    public float headScanAngle = 60f;   
    public float headScanSpeed = 1f;    

    [Header("Alerta a aliados")]
    public float allyAlertRadius = 10f;

    [Header("Ataque")]
    public float attackRange = 1.8f;
    [Range(10f, 360f)] public float attackAngle = 90f;
    public bool instantKill = true;
    public float attackDamage = 10f;   
    public float attackWindup = 0.5f;  
    public float attackCooldown = 1.5f;

    [Header("Recompensa")]
    public int points = 10;
}