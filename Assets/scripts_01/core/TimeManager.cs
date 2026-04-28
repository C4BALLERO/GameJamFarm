using UnityEngine;

/// <summary>
/// Manages game time, day/night cycles, and environmental time-based events.
/// </summary>
[DisallowMultipleComponent]
public sealed class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Settings")]
    [SerializeField] private float gameSpeedMultiplier = 1f;
    [SerializeField] private float dayDurationInMinutes = 20f;
    [SerializeField] private float sunriseTime = 6f;
    [SerializeField] private float sunsetTime = 18f;
    [SerializeField] private float difficultyRampPerMinute = 0.15f;

    private float _currentGameTime = 6f; // Start at 6 AM
    private float _dayCounter = 0;
    private float _elapsedSeconds = 0f;

    /// <summary>
    /// 1.0 at start, increases over time.
    /// </summary>
    public float DifficultyMultiplier { get; private set; } = 1f;
    public float ElapsedSeconds => _elapsedSeconds;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen)
            return;

        UpdateGameTime();
        UpdateDifficulty();
    }

    private void UpdateGameTime()
    {
        _elapsedSeconds += Time.deltaTime;
        _currentGameTime += (Time.deltaTime * gameSpeedMultiplier) / (dayDurationInMinutes * 60f) * 24f;

        if (_currentGameTime >= 24f)
        {
            _currentGameTime -= 24f;
            _dayCounter++;
            Debug.Log($"[TimeManager] New day! Day #{_dayCounter + 1}");
        }
    }

    private void UpdateDifficulty()
    {
        var minutes = _elapsedSeconds / 60f;
        DifficultyMultiplier = 1f + minutes * Mathf.Max(0f, difficultyRampPerMinute);
    }

    public float GetCurrentHour() => _currentGameTime;
    public int GetDayCount() => (int)_dayCounter;
    public bool IsNight() => _currentGameTime < sunriseTime || _currentGameTime >= sunsetTime;
    public bool IsDay() => !IsNight();
}

