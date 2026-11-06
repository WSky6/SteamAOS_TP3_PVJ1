using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private bool pauseGame = true;
    [SerializeField] private bool oneTimeOnly = true;

    private bool activated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (activated && oneTimeOnly)
            return;

        if (other.CompareTag("Player"))
        {
            activated = true;
            tutorialPanel.SetActive(true);
            GameManager2.Instance.ChangeState(GameState.Tutorial);
        }
    }


    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
        GameManager2.Instance.ChangeState(GameState.Playing);
    }

}