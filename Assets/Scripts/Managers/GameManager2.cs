using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    Start,
    Playing,
    Paused,
    Tutorial,
    Victory,
    GameOver,
}

public class GameManager2 : MonoBehaviour
{
    public static GameManager2 Instance { get; private set; }

    [Header("State Settings")]
    [SerializeField] private GameState currentState;
    public GameState CurrentState => currentState;

    [Header("Input System")]
    [Tooltip("Asigna aquí la acción de Pausa (ej. tecla P) desde tu Input Action Asset")]
    public InputActionReference pauseAction;

    [Header("Victory Settings")]
    //[SerializeField] private EnemyAI[] enemies;
    [SerializeField] private int totalEnemies;
    [SerializeField] private int enemiesRemaining;

    public event Action<GameState> OnChangeState;
    public event Action<int> OnEnemyChange;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;

        if (scene.name == "MenuPrincipal")
        {
            ChangeState(GameState.Start);
        }
        else
        {
            enemiesRemaining = totalEnemies;
            OnEnemyChange?.Invoke(enemiesRemaining);
            ChangeState(GameState.Playing);
        }
    }

    void Start()
    {
        enemiesRemaining = totalEnemies ;

        OnEnemyChange?.Invoke(enemiesRemaining);
        ChangeState(GameState.Playing);

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("PAUSA: tecla detectada");
        Debug.Log("Estado actual: " + currentState);

        Paused();

        Debug.Log("Estado posterior: " + currentState);
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState && newState != GameState.Playing) return;
        currentState = newState;
        
        switch (currentState)
        {
            case GameState.Start:
                Time.timeScale = 1f;
                Debug.Log("Inicio el juego");
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                Debug.Log("<color=green>ESTADO: jugando</color>");
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                Debug.Log("<color=yellow>ESTADO: pausa</color>");
                break;
            case GameState.Tutorial:
                Time.timeScale = 0f;
                Debug.Log("<color=blue>ESTADO: Tutorial</color>");
                break;
            case GameState.Victory:
                Time.timeScale = 0f;
                Debug.Log("<color=cyan>ESTADO: Victoria</color>");
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                Debug.Log("<color=red>ESTADO: Game Over</color>");
                break;
        }

        OnChangeState?.Invoke(currentState);
    }

    public void Paused()
    {
        if (currentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
        }
    }

    public void NotifyPlayerDead()
    {
        if (currentState != GameState.Playing) return;

        ChangeState(GameState.GameOver);
    }

    public void NotifyEnemyDefeat()
    {
        if (currentState != GameState.Playing) return;

        enemiesRemaining--;

        OnEnemyChange?.Invoke(enemiesRemaining);

        if (enemiesRemaining <= 0)
        {
            ChangeState(GameState.Victory);
        }
    }

    public void ResetGame()
    {
        Time.timeScale = 1f;

        enemiesRemaining = totalEnemies;

        ChangeState(GameState.Start);

        Debug.Log("Datos del juego reiniciados");
    }

    
}