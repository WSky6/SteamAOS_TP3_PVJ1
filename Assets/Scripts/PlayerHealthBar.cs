using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthSlider;

    void Awake()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealthBar: ¡No se encontró ningún PlayerHealth en la escena!", gameObject);
        }
    }

    void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += ActualizarBarra;
            ActualizarBarra(playerHealth.Current, playerHealth.Max);
            Debug.Log("PlayerHealthBar: Conectado correctamente al evento de salud.");
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= ActualizarBarra;
        }
    }

    private void ActualizarBarra(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = max;
            healthSlider.value = current;
            Debug.Log($"PlayerHealthBar: Barra actualizada. Actual: {current} / Max: {max}");
        }
        else
        {
            Debug.LogError("PlayerHealthBar: ¡El Slider asignado es nulo!", gameObject);
        }
    }
}