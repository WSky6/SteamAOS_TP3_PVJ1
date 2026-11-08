using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStats",
    menuName = "Scriptable Objects/PlayerStats"
)]
public class PlayerStats : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;

    [HideInInspector]
    public float currentHealth = 100f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.2f;

    [Header("Game Progress")]
    public int currentScore;
    public int currentLevel = 1;

    [Header("References")]
    public MeshRenderer skin;
    public GameObject prefab;

    public void ResetStats()
    {
        currentHealth = maxHealth;
        currentScore = 0;
        currentLevel = 1;
    }
}