using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class UIManager : MonoBehaviour
{
    //paneles
    [SerializeField] private GameObject PausePanel;
    [SerializeField] private GameObject VictoryPanel;
    [SerializeField] private GameObject GameOverPanel;
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private Slider sliderBar;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    private bool subscribed;

    //referencia healthsystem
    [SerializeField] private PlayerHealth healthSystemPlayer;
    //suscripciones estado y vida jugador
    //on destroy y start

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnChangeState -= StateChage;
            GameManager.Instance.OnEnemyChange -= UpdateEnemiesHUD;
        }
        subscribed = false;

        if (healthSystemPlayer != null)
            healthSystemPlayer.OnHealthChanged -= UpdateBar;
    }

    private void Start()
    {
        Subscribe();
        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        mainMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);

        if (volumeSlider != null)
            volumeSlider.value = volume;

        // Sincronizar con el estado actual por si nos perdimos el evento
        if (GameManager.Instance != null)
            StateChage(GameManager.Instance.CurrentState);
    }

    private void Subscribe()
    {
        if (subscribed) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnChangeState += StateChage;
            GameManager.Instance.OnEnemyChange += UpdateEnemiesHUD;
            subscribed = true;
            Debug.Log("UIManager suscrito a GameManager2 en " + SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.LogWarning("UIManager: GameManager2.Instance es null, no se pudo suscribir");
        }

        if (healthSystemPlayer != null)
            healthSystemPlayer.OnHealthChanged += UpdateBar;
    }

    

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnChangeState -= StateChage;
            GameManager.Instance.OnEnemyChange -= UpdateEnemiesHUD;
        }

        if (healthSystemPlayer != null)
        {
            healthSystemPlayer.OnHealthChanged -= UpdateBar;
        }
    }

    


    private void StateChage(GameState newState)
    {

        if (SceneManager.GetActiveScene().name == "MenuPrincipal")
        return;
        Debug.Log("UIManager recibido estado: " + newState);
  
        if (PausePanel != null) PausePanel.SetActive(false);

        VictoryPanel.SetActive(false);
        GameOverPanel.SetActive(false);


        switch (newState)
        {
            case GameState.Paused:
                PausePanel.SetActive(true);
                UnlockCursor();
                break;
            case GameState.Tutorial:
                UnlockCursor();
                break;
            case GameState.Victory:
                VictoryPanel.SetActive(true);
                UnlockCursor();
                break;
            case GameState.GameOver:
                GameOverPanel.SetActive(true);
                UnlockCursor();
                break;
            case GameState.Playing:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
        }
    }

    public void DeadChange()
    {
        GameManager.Instance.ChangeState(GameState.GameOver);
    }


    //metodos para las suscripciones

    //update bar update enemies info
    private void UpdateEnemiesHUD(int currentEnemies)
    {
        if (textMesh != null)
        {
            //textMesh.text = currentEnemies.ToString();
            textMesh.text = $"{currentEnemies}";
        }
    }

    private void UpdateBar(float health, float maxHealth)
    {
        if (sliderBar != null)
        {
            sliderBar.maxValue = maxHealth;
            sliderBar.value = health;
        }
    }



    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    //metodos para botones UI

    //exit

    //toggle panel
    public void ChangeScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void UntogglePause()
    {
        GameManager.Instance.ChangeState(GameState.Playing);
    }

    public void TogglePanel(GameObject panel)
    {
        if (panel != null)
        {
            bool toggle = panel.activeInHierarchy ? false : true;
            panel.SetActive(toggle);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }


    public void SetQuality(int qualityIndex)
    {

        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetResolution(int width, int height, bool isFullscreen)
    {
        Screen.SetResolution(width, height, isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
    }

    public void SetVSync(bool isEnabled)
    {
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
    }

    public void SetMasterVolume(float sliderValue)
    {
        // El AudioMixer usa decibelios (escala logaritmica), no valores lineales
        // Esta formula matematica convierte el valor del slider (0 a 1) a decibelios 
        mainMixer.SetFloat("MasterVolume", Mathf.Log10(sliderValue) * 20);

    PlayerPrefs.SetFloat("MasterVolume", sliderValue);
    }

    public void ReturnToMainMenu(string sceneName)
    {
        Time.timeScale = 1f;
        GameManager.Instance.ResetGame();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(sceneName);
    }

    public void ShowHint(TutorialHint hint)
    {

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        if (tutorialText != null)
        {
            tutorialText.text = hint.hintText;
        }
    }

    public void HideHint()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
            GameManager.Instance.ChangeState(GameState.Playing);
        }
    }
}

