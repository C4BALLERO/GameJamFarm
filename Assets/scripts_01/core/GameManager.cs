using UnityEngine;
using System;

/// <summary>
/// Core game manager that handles game state, initialization, and global systems.
/// Implements singleton pattern for easy access throughout the game.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private InventorySystem inventorySystem;

    [Header("Game State")]
    [SerializeField] private bool isPaused = false;
    
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;

    public TimeManager Time => timeManager;
    public InventorySystem Inventory => inventorySystem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GameManager] Initialized successfully");
    }

    private void Update()
    {
        HandlePauseInput();
    }

    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;

            if (isPaused)
                OnGamePaused?.Invoke();
            else
                OnGameResumed?.Invoke();
        }
    }

    private void Reset()
    {
        timeManager = GetComponent<TimeManager>();
        inventorySystem = GetComponent<InventorySystem>();
    }

    public bool IsPaused => isPaused;

    public void ResetGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameManager] Game reset");
    }
}

