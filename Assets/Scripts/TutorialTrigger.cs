using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private TutorialHint hint;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private bool oneTimeOnly = true;

    private bool activated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (activated && oneTimeOnly)
            return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Entró al trigger");
            uiManager.ShowHint(hint);
            GameManager2.Instance.ChangeState(GameState.Tutorial);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            uiManager.HideHint();
            GameManager2.Instance.ChangeState(GameState.Playing);
        }
    }
}