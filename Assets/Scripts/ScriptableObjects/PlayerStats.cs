using UnityEditor.VersionControl;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [Header("Estadísticas Base")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 5f;
    [Header("Progreso de Partida")]
    public int currentScore;
    public int currentLevel = 1;

    public MeshRenderer skin;
    public Asset prefab;

    public void ResetStats()
    {
        currentHealth = maxHealth;
        currentScore = 0;
        currentLevel = 1;
    }
}
