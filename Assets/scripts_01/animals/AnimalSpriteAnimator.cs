using UnityEngine;

/// <summary>
/// Simple looped walk animation for farm animals.
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimalSpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float walkFps = 7f;

    private int _frame;
    private float _nextFrameAt;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyFrame();
    }

    private void Update()
    {
        if (walkFrames == null || walkFrames.Length == 0) return;
        if (Time.time < _nextFrameAt) return;
        _nextFrameAt = Time.time + 1f / Mathf.Max(1f, walkFps);
        _frame = (_frame + 1) % walkFrames.Length;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (spriteRenderer == null) return;
        if (walkFrames == null || walkFrames.Length == 0) return;
        spriteRenderer.sprite = walkFrames[Mathf.Clamp(_frame, 0, walkFrames.Length - 1)];
    }
}
