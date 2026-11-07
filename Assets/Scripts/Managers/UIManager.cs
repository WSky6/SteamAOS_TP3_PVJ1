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

    //referencia healthsystem
    [SerializeField] private HealthSystem healthSystemPlayer;
    //suscripciones estado y vida jugador
    //on destroy y start
    private void Start()
    {
        if (GameManager2.Instance != null)
        {
            GameManager2.Instance.OnChangeState += StateChage;
            GameManager2.Instance.OnEnemyChange += UpdateEnemiesHUD;
        }

        /*
        if (healthSystemPlayer != null)
        {
            healthSystemPlayer.OnChangeHealthPlayer += UpdateBar;
        }*/

        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);

        mainMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);

        if (volumeSlider != null)
        {
            volumeSlider.value = volume;
        }
    }

    

    private void OnDestroy()
    {
        if (GameManager2.Instance != null)
        {
            GameManager2.Instance.OnChangeState -= StateChage;
            GameManager2.Instance.OnEnemyChange -= UpdateEnemiesHUD;
        }

        /*
        if (healthSystemPlayer != null)
        {
            healthSystemPlayer.OnChangeHealthPlayer -= UpdateBar;
        }*/
    }


    private void StateChage(GameState newState)
    {
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

    private void UpdateBar(float health)
    {
        if (sliderBar != null)
        {
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
    public void ChangeScene(SceneAsset scene)
    {
        SceneManager.LoadScene(scene.name);
        //GameManager2.Instance.
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
        Debug.Log("Volumen: " + sliderValue);
        // El AudioMixer usa decibelios (escala logaritmica), no valores lineales
        // Esta formula matematica convierte el valor del slider (0 a 1) a decibelios 
    mainMixer.SetFloat("MasterVolume", Mathf.Log10(sliderValue) * 20);

    PlayerPrefs.SetFloat("MasterVolume", sliderValue);
    }

    public void ReturnToMainMenu(string sceneName)
    {
        GameManager2.Instance.ResetGame();
        SceneManager.LoadScene(sceneName);
    }
}

