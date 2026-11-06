using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    [SerializeField] private string nextSceneName;

    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;

        if (other.CompareTag("Player"))
        {
            activated = true;

            Time.timeScale = 1f;

            SceneManager.LoadScene(nextSceneName);
        }
    }
}