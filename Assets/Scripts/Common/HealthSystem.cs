using System;
using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]

    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private EnemyStats enemyStats;


    [SerializeField] private bool isDead;
    public bool IsDead
    {
        get => isDead;

    }

    [SerializeField] private GameObject vfx;

    private Rigidbody rb;
    private Collider col;
    //private Attack attack;
    private MeshRenderer mesh;

    public event Action<float> OnChangeHealthPlayer;
    private void Awake()
    {
        isDead = false;
        playerStats.ResetStats();
        enemyStats.ResetStats();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        //attack = GetComponent<Attack>();
        mesh = GetComponent<MeshRenderer>();
    }
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (gameObject.CompareTag("Player"))
        {
            playerStats.currentHealth -= amount;
            OnChangeHealthPlayer?.Invoke(playerStats.currentHealth);

            if (playerStats.currentHealth <= 0f)
            {
                Dead();
            }
        }

        if (gameObject.CompareTag("Character"))
        {
            enemyStats.currentHealth -= amount;

            if (enemyStats.currentHealth <= 0f)
            {
                Dead();
            }
        }


    }
    private void Dead()
    {
        isDead = true;

        if (gameObject.CompareTag("Player"))
        {
            GameManager2.Instance.NotifyPlayerDead();
        }
        else if (gameObject.CompareTag("Character"))
        {
            GameManager2.Instance.NotifyEnemyDefeat();
        }

        vfx.SetActive(true);
        StartCoroutine(DestroyTransition());

        Debug.Log("Character eliminado");
    }

    IEnumerator DestroyTransition()
    {
        col.enabled = false;
        rb.isKinematic = true;
        mesh.enabled = false;
        //attack.enabled = false;

        yield return new WaitForSeconds(5f);

        Destroy(this.gameObject);
    }

}
