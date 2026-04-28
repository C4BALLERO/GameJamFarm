using UnityEngine;

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
    [SerializeField] private int maxAlive = 14;

    [Header("Spawn Rate")]
    [SerializeField] private float baseSpawnInterval = 5.5f;
    [SerializeField] private float minSpawnInterval = 1.35f;
    [SerializeField] private float intervalDecreasePerMinute = 0.06f;

    private float _nextSpawnAt;
    private int _alive;

    /// <summary>
    /// Set the player transform for distance checks
    /// </summary>
    public void SetPlayer(Transform t) => player = t;

    /// <summary>Assign difficulty clock reference at runtime if not wired in the inspector.</summary>
    public void SetTimeManager(TimeManager tm) => timeManager = tm;

    /// <summary>Optional: wire so border spawns skip corral colliders.</summary>
    public void SetCorralManager(CorralManager cm) => corralManager = cm;

    private void Reset()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen) return;
        if (player == null) return;
        if (corralManager == null)
            corralManager = CorralManager.Instance;
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (_alive >= maxAlive) return;
        if (Time.time < _nextSpawnAt) return;

        _nextSpawnAt = Time.time + ComputeInterval();
        TrySpawn();
    }

    /// <summary>
    /// Compute the current spawn interval based on elapsed time
    /// </summary>
    private float ComputeInterval()
    {
        var minutes = (timeManager != null) ? timeManager.ElapsedSeconds / 60f : 0f;
        var interval = baseSpawnInterval - minutes * Mathf.Max(0f, intervalDecreasePerMinute);
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

            _alive++;
            enemy.Died += () => _alive = Mathf.Max(0, _alive - 1);
            Debug.Log($"[SpawnManager] Spawned enemy. Alive: {_alive}/{maxAlive}");
            return;
        }
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
