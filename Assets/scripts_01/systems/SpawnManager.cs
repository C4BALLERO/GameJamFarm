using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpawnManager : MonoBehaviour
{
    [Header("Refs")]
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

    [Header("Rate")]
    [SerializeField] private float baseSpawnInterval = 2.0f;
    [SerializeField] private float minSpawnInterval = 0.35f;
    [SerializeField] private float intervalDecreasePerMinute = 0.25f;

    private float _nextSpawnAt;
    private int _alive;

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

    private float ComputeInterval()
    {
        var minutes = (timeManager != null) ? timeManager.ElapsedSeconds / 60f : 0f;
        var interval = baseSpawnInterval - minutes * Mathf.Max(0f, intervalDecreasePerMinute);
        return Mathf.Clamp(interval, minSpawnInterval, 999f);
    }

    private void TrySpawn()
    {
        const int maxAttempts = 20;
        for (var i = 0; i < maxAttempts; i++)
        {
            var pos = RandomPointInRect(center, size);
            if (Vector2.Distance(pos, player.position) < minDistanceFromPlayer) continue;

            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var enemy = Instantiate(prefab, pos, Quaternion.identity);

            // AI points to player
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null) ai.SetTarget(player);

            _alive++;
            enemy.Died += () => _alive = Mathf.Max(0, _alive - 1);
            return;
        }
    }

    private static Vector2 RandomPointInRect(Vector2 rectCenter, Vector2 rectSize)
    {
        var half = rectSize * 0.5f;
        return new Vector2(
            Random.Range(rectCenter.x - half.x, rectCenter.x + half.x),
            Random.Range(rectCenter.y - half.y, rectCenter.y + half.y)
        );
    }
}

