using UnityEngine;
using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    /// <summary>Scene time / difficulty clock (not UnityEngine.Time).</summary>
    public TimeManager WorldTime => timeManager;
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
        if (WasEscapePressed())
        {
            isPaused = !isPaused;
            UnityEngine.Time.timeScale = isPaused ? 0f : 1f;

            if (isPaused)
                OnGamePaused?.Invoke();
            else
                OnGameResumed?.Invoke();
        }
    }

    private static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void Reset()
    {
        timeManager = GetComponent<TimeManager>();
        inventorySystem = GetComponent<InventorySystem>();
    }

    public bool IsPaused => isPaused;

    public void ResetGame()
    {
        UnityEngine.Time.timeScale = 1f;
        Debug.Log("[GameManager] Game reset");
    }
}

