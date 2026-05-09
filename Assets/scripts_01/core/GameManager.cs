using UnityEngine;
using System;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Gestiona pausa global (Escape) y pausa por tienda (Granero).</summary>
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private InventorySystem inventorySystem;

    [Header("Game State")]
    [SerializeField] private bool pausedFromEscape;

    /// <summary>Pausa por panel de tienda abierto.</summary>
    private bool _pausedFromShop;

    /// <summary>Granero destruido: fin de partida.</summary>
    private bool _gameOver;

    public static event Action OnGamePaused;
    public static event Action OnGameResumed;

    public TimeManager WorldTime => timeManager;
    public InventorySystem Inventory => inventorySystem;

    /// <summary>Cuando hay pausa por Escape, tienda abierta o game over.</summary>
    public bool IsGameplayFrozen => _gameOver || pausedFromEscape || _pausedFromShop;

    public bool IsGameOver => _gameOver;

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

    private void Update()
    {
        HandlePauseInput();
    }

    private void HandlePauseInput()
    {
        if (_gameOver)
            return;

        if (!WasEscapePressed())
            return;

        if (_pausedFromShop)
        {
            var shop = FindFirstObjectByType<ShopUI>();
            shop?.Close();
            return;
        }

        pausedFromEscape = !pausedFromEscape;
        SyncTimeScale();
        if (pausedFromEscape)
            OnGamePaused?.Invoke();
        else
            OnGameResumed?.Invoke();
    }

    /// <summary>Llamado desde <see cref="ShopUI"/> al abrir/cerrar el Granero.</summary>
    public void SetShopPaused(bool paused)
    {
        _pausedFromShop = paused;
        SyncTimeScale();
        if (paused)
            OnGamePaused?.Invoke();
        else
            OnGameResumed?.Invoke();
    }

    private void SyncTimeScale()
    {
        UnityEngine.Time.timeScale = IsGameplayFrozen ? 0f : 1f;
    }

    /// <summary>Llamado cuando el granero llega a 0 de vida.</summary>
    public void EnterGameOver()
    {
        if (_gameOver)
            return;
        _gameOver = true;
        pausedFromEscape = false;
        _pausedFromShop = false;
        SyncTimeScale();
    }

    /// <summary>Reinicia la escena actual tras game over.</summary>
    public void RestartFromGameOver()
    {
        _gameOver = false;
        pausedFromEscape = false;
        _pausedFromShop = false;
        UnityEngine.Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMainMenuFromGameOver()
    {
        _gameOver = false;
        UnityEngine.Time.timeScale = 1f;
        SceneManager.LoadScene(0);
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

    public bool IsPaused => pausedFromEscape;

    public void ResumeFromPause()
    {
        if (!pausedFromEscape) return;
        pausedFromEscape = false;
        SyncTimeScale();
        OnGameResumed?.Invoke();
    }

    public void ResetGame()
    {
        _gameOver = false;
        pausedFromEscape = false;
        _pausedFromShop = false;
        UnityEngine.Time.timeScale = 1f;
    }
}
