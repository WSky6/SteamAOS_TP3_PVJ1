using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Scriptable Objects/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    public float maxHealth = 100f;
    public float currentHealth;
    public float speed;
    public float damage;
    public float RadiusDetection;

    public MeshRenderer skin;

    public AudioClip soundDetection;

    public void ResetStats()
    {
        currentHealth = maxHealth;

    }
}
