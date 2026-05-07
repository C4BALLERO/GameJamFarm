using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls a timer-based day/night cycle, smooth ambient transitions,
/// and exposes phase events for gameplay systems.
/// </summary>
[DisallowMultipleComponent]
public sealed class DayNightManager : MonoBehaviour
{
    public enum CycleState
    {
        Day = 0,
        Night = 1
    }

    [Header("Cycle Durations (seconds)")]
    [SerializeField] private float dayDurationSeconds = 120f;
    [SerializeField] private float nightDurationSeconds = 90f;

    [Header("UI")]
    [SerializeField] private Text phaseText;
    [SerializeField] private Image sunIcon;
    [SerializeField] private Image moonIcon;
    [SerializeField] private Image clockFill;
    [SerializeField] private RectTransform clockHand;
    [SerializeField] private Text clockText;

    [Header("Visual")]
    [Tooltip("Optional fullscreen sprite/quad to darken the scene.")]
    [SerializeField] private SpriteRenderer darknessOverlay;
    [SerializeField] private Light2D globalLight2D;
    [SerializeField] [Range(0f, 1f)] private float dayOverlayAlpha = 0f;
    [SerializeField] [Range(0f, 1f)] private float nightOverlayAlpha = 0.52f;
    [SerializeField] private float transitionSpeed = 1.9f;
    [SerializeField] private Color overlayColor = Color.black;
    [SerializeField] private Color dayLightColor = new(1f, 0.98f, 0.9f, 1f);
    [SerializeField] private Color nightLightColor = new(0.35f, 0.42f, 0.62f, 1f);
    [SerializeField] private float dayLightIntensity = 0.95f;
    [SerializeField] private float nightLightIntensity = 0.35f;

    [Header("Night Difficulty")]
    [SerializeField] private float baseNightDifficulty = 1f;
    [SerializeField] private float extraDifficultyPerNight = 0.18f;
    [SerializeField] private float extraDifficultyAcrossNight = 0.28f;

    public static DayNightManager Instance { get; private set; }

    public event Action OnDayStarted;
    public event Action OnNightStarted;

    public CycleState CurrentState { get; private set; } = CycleState.Day;
    public int CompletedNights { get; private set; }
    public float PhaseElapsedSeconds { get; private set; }
    public bool IsDay => CurrentState == CycleState.Day;
    public bool IsNight => CurrentState == CycleState.Night;

    private float CurrentPhaseDuration => IsDay
        ? Mathf.Max(1f, dayDurationSeconds)
        : Mathf.Max(1f, nightDurationSeconds);

    private float PhaseNormalized => Mathf.Clamp01(PhaseElapsedSeconds / CurrentPhaseDuration);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        CurrentState = CycleState.Day;
        PhaseElapsedSeconds = 0f;
        ApplyVisualImmediate();
        RefreshText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen)
            return;

        PhaseElapsedSeconds += Time.deltaTime;
        if (PhaseElapsedSeconds >= CurrentPhaseDuration)
            SwitchPhase();

        ApplyVisualSmooth();
        ApplyLightSmooth();
        RefreshClockUi();
    }

    public float GetNightDifficultyMultiplier()
    {
        var fullNights = CompletedNights * Mathf.Max(0f, extraDifficultyPerNight);
        var thisNight = IsNight ? PhaseNormalized * Mathf.Max(0f, extraDifficultyAcrossNight) : 0f;
        return Mathf.Max(1f, baseNightDifficulty + fullNights + thisNight);
    }

    private void SwitchPhase()
    {
        PhaseElapsedSeconds = 0f;
        if (IsDay)
        {
            CurrentState = CycleState.Night;
            OnNightStarted?.Invoke();
            RefreshText();
            return;
        }

        CurrentState = CycleState.Day;
        CompletedNights++;
        OnDayStarted?.Invoke();
        RefreshText();
    }

    private void RefreshText()
    {
        // Sun/moon swap siempre, independiente de si hay phaseText
        if (sunIcon  != null) sunIcon.enabled  = IsDay;
        if (moonIcon != null) moonIcon.enabled = IsNight;

        if (phaseText != null)
            phaseText.text = IsDay ? "DAY" : "NIGHT";
    }

    private void ApplyVisualImmediate()
    {
        if (darknessOverlay == null)
            return;

        var c = overlayColor;
        c.a = IsDay ? dayOverlayAlpha : nightOverlayAlpha;
        darknessOverlay.color = c;
    }

    private void ApplyVisualSmooth()
    {
        if (darknessOverlay == null)
            return;

        var targetAlpha = IsDay ? dayOverlayAlpha : nightOverlayAlpha;
        var c = darknessOverlay.color;
        c.r = overlayColor.r;
        c.g = overlayColor.g;
        c.b = overlayColor.b;
        c.a = Mathf.MoveTowards(c.a, targetAlpha, Mathf.Max(0.05f, transitionSpeed) * Time.deltaTime);
        darknessOverlay.color = c;
    }

    private void ApplyLightSmooth()
    {
        if (globalLight2D == null)
            globalLight2D = FindFirstObjectByType<Light2D>();
        if (globalLight2D == null)
            return;

        var targetIntensity = IsDay ? dayLightIntensity : nightLightIntensity;
        globalLight2D.intensity = Mathf.MoveTowards(
            globalLight2D.intensity,
            targetIntensity,
            Mathf.Max(0.05f, transitionSpeed) * Time.deltaTime);
        var t = Mathf.Clamp01(Mathf.Max(0.01f, transitionSpeed) * Time.deltaTime);
        var targetColor = IsDay ? dayLightColor : nightLightColor;
        globalLight2D.color = Color.Lerp(globalLight2D.color, targetColor, t);
    }

    private void RefreshClockUi()
    {
        var normalized = PhaseNormalized;
        if (clockFill != null)
            clockFill.fillAmount = normalized;
        if (clockHand != null)
            clockHand.localEulerAngles = new Vector3(0f, 0f, -normalized * 360f);
        if (clockText != null)
        {
            var remain = Mathf.CeilToInt(Mathf.Max(0f, CurrentPhaseDuration - PhaseElapsedSeconds));
            clockText.text = $"{(IsDay ? "Day" : "Night")} {remain}s";
        }
    }
}

