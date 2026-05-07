using UnityEngine;
using System;
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

    public static event Action OnGamePaused;
    public static event Action OnGameResumed;

    public TimeManager WorldTime => timeManager;
    public InventorySystem Inventory => inventorySystem;

    /// <summary>Cuando hay pausa por Escape o tienda abierta.</summary>
    public bool IsGameplayFrozen => pausedFromEscape || _pausedFromShop;

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
        pausedFromEscape = false;
        _pausedFromShop = false;
        UnityEngine.Time.timeScale = 1f;
    }
}
