using UnityEngine;

public sealed class TimeManager : MonoBehaviour
{
    [SerializeField] private float difficultyRampPerMinute = 0.15f;

    public float ElapsedSeconds { get; private set; }

    /// <summary>
    /// 1.0 at start, increases over time.
    /// </summary>
    public float DifficultyMultiplier { get; private set; } = 1f;

    private void Update()
    {
        ElapsedSeconds += Time.deltaTime;
        var minutes = ElapsedSeconds / 60f;
        DifficultyMultiplier = 1f + minutes * Mathf.Max(0f, difficultyRampPerMinute);
    }
}

