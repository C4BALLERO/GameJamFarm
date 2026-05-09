using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Manages spawning of enemies in the game world.
/// Respects spawn area bounds, player distance, and spawn rate limits.
/// Difficulty increases over time by reducing spawn intervals.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private DayNightManager dayNightManager;
    [SerializeField] private CorralManager corralManager;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 center = Vector2.zero;
    [SerializeField] private Vector2 size = new(20f, 20f);
    [SerializeField] private bool spawnOnBordersOnly = true;
    [SerializeField] private float borderInset = 0.15f;
    [SerializeField] private float minDistanceFromPlayer = 4f;

    [Header("Prefabs")]
    [SerializeField] private EnemyBase[] enemyPrefabs;

    [Header("Limits")]
    [SerializeField] private int maxAlive = 40;

    [Header("Cap por noche (enemigos vivos a la vez)")]
    [Tooltip("Noche 1: máximo concurrente. Cada noche siguiente suma el extra.")]
    [SerializeField] private int firstNightMaxAlive = 8;
    [SerializeField] private int extraMaxAlivePerNight = 5;

    [Header("Spawn Rate")]
    [SerializeField] private float baseSpawnInterval = 12.0f;   // Noche 1: muy lento
    [SerializeField] private float minSpawnInterval = 1.2f;
    [SerializeField] private float intervalDecreasePerMinute = 0.04f;

    [Header("Wave System")]
    [SerializeField] private float waveDurationSeconds = 45f;
    [SerializeField] private int baseWaveMaxAlive = 2;           // Noche 1: solo 2 enemigos
    [SerializeField] private int extraAlivePerWave = 1;
    [SerializeField] private Text waveCounterText;

    [Header("Night Enemy Scaling")]
    [SerializeField] private float enemyHealthPerNight = 0.20f;  // +20% vida por noche
    [SerializeField] private float enemyDamagePerNight = 0.15f;  // +15% daño por noche
    [SerializeField] private int maxNightScalingSteps = 15;
    [SerializeField] private float nightIntervalReduction = 1.4f; // intervalo baja 1.4s por noche

    [Header("Day Cleanup")]
    [Tooltip("During day, surviving enemies are removed gradually.")]
    [SerializeField] private bool despawnSurvivorsDuringDay = true;
    [SerializeField] private float dayDespawnInterval = 4.5f;
    [SerializeField] private int dayDespawnDamage = 999;

    private float _nextSpawnAt;
    private int _alive;
    private float _nextDayCullAt;
    private readonly List<EnemyBase> _spawned = new();
    private float _waveStartedAt;
    private int _waveIndex = 1;
    private int _nightIndex;
    private bool _nightInitialized;

    /// <summary>
    /// Set the player transform for distance checks
    /// </summary>
    public void SetPlayer(Transform t) => player = t;

    /// <summary>Assign difficulty clock reference at runtime if not wired in the inspector.</summary>
    public void SetTimeManager(TimeManager tm) => timeManager = tm;

    public void SetDayNightManager(DayNightManager manager) => dayNightManager = manager;

    /// <summary>Optional: wire so border spawns skip corral colliders.</summary>
    public void SetCorralManager(CorralManager cm) => corralManager = cm;

    private void Reset()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
        dayNightManager = FindFirstObjectByType<DayNightManager>();
    }

    private void Start()
    {
        _waveStartedAt = Time.time;
        _waveIndex = 0;
        RefreshWaveUi();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen) return;
        if (player == null) return;
        if (dayNightManager == null)
            dayNightManager = DayNightManager.Instance ?? FindFirstObjectByType<DayNightManager>();
        if (dayNightManager != null && dayNightManager.IsDay)
        {
            _nightInitialized = false;
            HandleDaytimeEnemyCleanup();
            RefreshWaveUi();
            return; // Day rule: no enemy spawning.
        }
        if (!_nightInitialized)
            BeginNight();
        if (corralManager == null)
            corralManager = CorralManager.Instance;
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        UpdateWaveState();
        if (_alive >= GetCurrentWaveMaxAlive()) return;
        if (Time.time < _nextSpawnAt) return;

        _nextSpawnAt = Time.time + ComputeInterval();
        TrySpawn();
    }

    private void HandleDaytimeEnemyCleanup()
    {
        if (!despawnSurvivorsDuringDay)
            return;
        if (Time.time < _nextDayCullAt)
            return;

        _nextDayCullAt = Time.time + Mathf.Max(1f, dayDespawnInterval);
        CleanupDeadRefs();
        if (_spawned.Count == 0)
            return;

        var idx = Random.Range(0, _spawned.Count);
        var enemy = _spawned[idx];
        if (enemy == null || enemy.IsDead)
            return;

        enemy.TakeDamage(Mathf.Max(1, dayDespawnDamage), Vector2.zero);
    }

    /// <summary>
    /// Compute the current spawn interval based on elapsed time
    /// </summary>
    private float ComputeInterval()
    {
        var minutes = (timeManager != null) ? timeManager.ElapsedSeconds / 60f : 0f;
        // Cada noche el intervalo baja; el primer día es muy lento
        var nightReduction = Mathf.Max(0, _nightIndex - 1) * Mathf.Max(0f, nightIntervalReduction);
        var interval = baseSpawnInterval - nightReduction - minutes * Mathf.Max(0f, intervalDecreasePerMinute);
        interval *= Mathf.Max(0.55f, 1.3f - (_waveIndex - 1) * 0.09f);
        if (dayNightManager != null)
            interval /= Mathf.Max(1f, dayNightManager.GetNightDifficultyMultiplier());
        if (PowerUpSystem.Instance != null)
            interval *= Mathf.Max(0.02f, PowerUpSystem.Instance.EnemySpawnIntervalMultiplier);
        return Mathf.Clamp(interval, minSpawnInterval, 999f);
    }

    /// <summary>
    /// Attempt to spawn an enemy at a valid location
    /// </summary>
    private void TrySpawn()
    {
        const int maxAttempts = 20;
        for (var i = 0; i < maxAttempts; i++)
        {
            var pos = spawnOnBordersOnly
                ? RandomPointOnRectBorder(center, size, borderInset)
                : RandomPointInRect(center, size);

            if (corralManager != null && corralManager.IsPointInsideAnyCorral(pos))
                continue;

            if (Vector2.Distance(pos, player.position) < minDistanceFromPlayer)
                continue;

            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var enemy = Instantiate(prefab, pos, Quaternion.identity);
            ApplyNightScaling(enemy);

            _alive++;
            _spawned.Add(enemy);
            enemy.Died += () =>
            {
                _alive = Mathf.Max(0, _alive - 1);
                _spawned.Remove(enemy);
            };
            Debug.Log($"[SpawnManager] Spawned enemy. Alive: {_alive}/{GetCurrentWaveMaxAlive()}");
            return;
        }
    }

    private void CleanupDeadRefs()
    {
        for (var i = _spawned.Count - 1; i >= 0; i--)
        {
            var e = _spawned[i];
            if (e == null || e.IsDead)
                _spawned.RemoveAt(i);
        }
        _alive = Mathf.Clamp(_alive, 0, _spawned.Count + 2);
    }

    private void UpdateWaveState()
    {
        if (waveDurationSeconds <= 0f)
            return;

        var elapsed = Time.time - _waveStartedAt;
        if (elapsed < waveDurationSeconds)
            return;

        _waveStartedAt = Time.time;
        _waveIndex++;
        RefreshWaveUi();
    }

    private int GetCurrentWaveMaxAlive()
    {
        var nightCap = firstNightMaxAlive + Mathf.Max(0, _nightIndex - 1) * Mathf.Max(0, extraMaxAlivePerNight);
        nightCap = Mathf.Min(maxAlive, nightCap);
        return Mathf.Max(1, nightCap);
    }

    private void RefreshWaveUi()
    {
        if (waveCounterText == null)
            return;
        if (dayNightManager == null || dayNightManager.IsDay || _waveIndex <= 0)
        {
            waveCounterText.text = string.Empty;
            return;
        }

        waveCounterText.text = $"Oleada {_waveIndex}";
    }

    private void BeginNight()
    {
        _nightInitialized = true;
        _nightIndex++;
        _waveIndex = 1;
        _waveStartedAt = Time.time;
        RefreshWaveUi();
    }

    private void ApplyNightScaling(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        var steps = Mathf.Clamp(_nightIndex - 1, 0, Mathf.Max(0, maxNightScalingSteps));
        if (steps <= 0)
            return;

        var healthMultiplier = 1f + steps * Mathf.Max(0f, enemyHealthPerNight);
        var damageMultiplier = 1f + steps * Mathf.Max(0f, enemyDamagePerNight);
        enemy.ApplyDifficultyScaling(healthMultiplier, damageMultiplier);
    }

    /// <summary>
    /// Get a random point within a rectangle
    /// </summary>
    private static Vector2 RandomPointInRect(Vector2 rectCenter, Vector2 rectSize)
    {
        var half = rectSize * 0.5f;
        return new Vector2(
            Random.Range(rectCenter.x - half.x, rectCenter.x + half.x),
            Random.Range(rectCenter.y - half.y, rectCenter.y + half.y)
        );
    }

    private static Vector2 RandomPointOnRectBorder(Vector2 rectCenter, Vector2 rectSize, float inset)
    {
        var half = rectSize * 0.5f;
        var minX = rectCenter.x - half.x + inset;
        var maxX = rectCenter.x + half.x - inset;
        var minY = rectCenter.y - half.y + inset;
        var maxY = rectCenter.y + half.y - inset;

        var side = Random.Range(0, 4);
        return side switch
        {
            0 => new Vector2(Random.Range(minX, maxX), maxY), // top
            1 => new Vector2(Random.Range(minX, maxX), minY), // bottom
            2 => new Vector2(minX, Random.Range(minY, maxY)), // left
            _ => new Vector2(maxX, Random.Range(minY, maxY))  // right
        };
    }
}
