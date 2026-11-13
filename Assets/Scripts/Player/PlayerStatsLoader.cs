using UnityEngine;
using StarterAssets;

public class PlayerStatsLoader : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    private FirstPersonController controller;

    private void Awake()
    {
        controller = GetComponent<FirstPersonController>();

        if (controller == null || playerStats == null)
            return;

        controller.MoveSpeed = playerStats.moveSpeed;
        controller.SprintSpeed = playerStats.sprintSpeed;
        controller.JumpHeight = playerStats.jumpHeight;
    }
}