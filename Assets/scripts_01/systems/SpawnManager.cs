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

    [Header("Spawn Area")]
    [SerializeField] private Vector2 center = Vector2.zero;
    [SerializeField] private Vector2 size = new(20f, 20f);
    [SerializeField] private float minDistanceFromPlayer = 4f;

    [Header("Prefabs")]
    [SerializeField] private EnemyBase[] enemyPrefabs;

    [Header("Limits")]
    [SerializeField] private int maxAlive = 25;

    [Header("Spawn Rate")]
    [SerializeField] private float baseSpawnInterval = 2.0f;
    [SerializeField] private float minSpawnInterval = 0.35f;
    [SerializeField] private float intervalDecreasePerMinute = 0.25f;

    private float _nextSpawnAt;
    private int _alive;

    /// <summary>
    /// Set the player transform for distance checks
    /// </summary>
    public void SetPlayer(Transform t) => player = t;

    private void Reset()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
    }

    private void Update()
    {
        if (player == null) return;
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
            var pos = RandomPointInRect(center, size);
            if (Vector2.Distance(pos, player.position) < minDistanceFromPlayer) 
                continue;

            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var enemy = Instantiate(prefab, pos, Quaternion.identity);

            // Set AI target to player
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null) 
                ai.SetTarget(player);

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
}
        );
    }
}

