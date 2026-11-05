using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Start,
    Playing,
    Paused,
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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    void Start()
    {
        enemiesRemaining = totalEnemies ;

        OnEnemyChange?.Invoke(enemiesRemaining);
        ChangeState(GameState.Playing);
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        Paused();
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
}