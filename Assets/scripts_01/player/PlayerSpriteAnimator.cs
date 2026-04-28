using UnityEngine;

/// <summary>
/// Farmer sprite playback with Idle / Walk / Attack / Death — ties into sliced sheets (36 frames typical).
/// Works standalone or alongside an <see cref="Animator"/> using matching float/bool/trigger names.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerSpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Frames (slice Multiple sprite grid e.g. 6×6)")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] deathFrames;

    [Header("Timing")]
    [SerializeField] private float idleFps = 8f;
    [SerializeField] private float walkFps = 10f;
    [SerializeField] private float attackFps = 14f;
    [SerializeField] private float deathFps = 12f;
    [SerializeField] private bool faceWithFlipX = true;

    private enum AnimState
    {
        Idle,
        Walk,
        Attack,
        Dead
    }

    private AnimState _state = AnimState.Idle;
    private int _frame;
    private float _nextFrameAt;
    private Vector2 _lastFacing = Vector2.right;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        ApplyCurrentFrame();
    }

    public void SetMovement(Vector2 facing, bool moving)
    {
        if (_state == AnimState.Dead || _state == AnimState.Attack) return;
        if (facing.sqrMagnitude > 0.001f)
            _lastFacing = facing.normalized;

        _state = moving ? AnimState.Walk : AnimState.Idle;
    }

    public void TriggerAttack()
    {
        if (_state == AnimState.Dead) return;
        _state = AnimState.Attack;
        _frame = 0;
        _nextFrameAt = 0f;
        ApplyCurrentFrame();
    }

    public void TriggerDeath()
    {
        _state = AnimState.Dead;
        _frame = 0;
        _nextFrameAt = 0f;
        ApplyCurrentFrame();
    }

    public void TriggerRevive()
    {
        _state = AnimState.Idle;
        _frame = 0;
        _nextFrameAt = 0f;
        ApplyCurrentFrame();
    }

    private void Update()
    {
        var now = Time.time;
        if (now < _nextFrameAt) return;

        var fps = GetStateFps();
        _nextFrameAt = now + 1f / Mathf.Max(1f, fps);

        switch (_state)
        {
            case AnimState.Idle:
                if (idleFrames != null && idleFrames.Length > 0)
                    _frame = (_frame + 1) % idleFrames.Length;
                else
                    _frame = 0;
                break;

            case AnimState.Walk:
                if (walkFrames != null && walkFrames.Length > 0)
                    _frame = (_frame + 1) % walkFrames.Length;
                else
                    _frame = 0;
                break;

            case AnimState.Attack:
                _frame++;
                if (attackFrames == null || attackFrames.Length == 0)
                {
                    _state = AnimState.Idle;
                    _frame = 0;
                }
                else if (_frame >= attackFrames.Length)
                {
                    _state = AnimState.Idle;
                    _frame = 0;
                }
                break;

            case AnimState.Dead:
                if (deathFrames != null && deathFrames.Length > 0 && _frame < deathFrames.Length - 1)
                    _frame++;
                break;
        }

        ApplyCurrentFrame();
    }

    private float GetStateFps()
    {
        return _state switch
        {
            AnimState.Walk => walkFps,
            AnimState.Attack => attackFps,
            AnimState.Dead => deathFps,
            AnimState.Idle => idleFps,
            _ => walkFps
        };
    }

    private void ApplyCurrentFrame()
    {
        if (spriteRenderer == null) return;

        var frames = GetStateFrames();
        if (frames == null || frames.Length == 0) return;

        var idx = Mathf.Clamp(_frame, 0, frames.Length - 1);
        spriteRenderer.sprite = frames[idx];
        UpdateFacing();
    }

    private Sprite[] GetStateFrames()
    {
        return _state switch
        {
            AnimState.Attack when attackFrames != null && attackFrames.Length > 0 => attackFrames,
            AnimState.Dead when deathFrames != null && deathFrames.Length > 0 => deathFrames,
            AnimState.Idle when idleFrames != null && idleFrames.Length > 0 => idleFrames,
            AnimState.Walk when walkFrames != null && walkFrames.Length > 0 => walkFrames,
            AnimState.Idle => walkFrames,
            _ => walkFrames
        };
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null || !faceWithFlipX) return;
        if (Mathf.Abs(_lastFacing.x) < 0.001f) return;
        spriteRenderer.flipX = _lastFacing.x < 0f;
    }
}
