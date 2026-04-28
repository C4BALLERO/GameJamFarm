using UnityEngine;

/// <summary>
/// Lightweight frame animator for enemies using sliced sprite sheets.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemySpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float idleFps = 6f;
    [SerializeField] private float walkFps = 8f;
    [SerializeField] private float attackFps = 12f;
    [SerializeField] private float deathFps = 10f;
    [SerializeField] private bool faceWithFlipX = true;

    private enum State
    {
        Idle,
        Walk,
        Attack,
        Dead
    }

    private State _state = State.Idle;
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
        ApplyFrame();
    }

    public void SetMoveState(Vector2 facing, bool moving)
    {
        if (_state == State.Dead || _state == State.Attack) return;
        if (facing.sqrMagnitude > 0.001f) _lastFacing = facing.normalized;
        _state = moving ? State.Walk : State.Idle;
        UpdateFlip();
    }

    public void TriggerAttack()
    {
        if (_state == State.Dead) return;
        if (attackFrames == null || attackFrames.Length == 0) return;
        _state = State.Attack;
        _frame = 0;
        _nextFrameAt = 0f;
        ApplyFrame();
    }

    public void TriggerDeath()
    {
        _state = State.Dead;
        _frame = 0;
        _nextFrameAt = 0f;
        ApplyFrame();
    }

    private void Update()
    {
        if (Time.time < _nextFrameAt) return;
        _nextFrameAt = Time.time + 1f / Mathf.Max(1f, GetFps());

        switch (_state)
        {
            case State.Idle:
                if (idleFrames != null && idleFrames.Length > 0)
                    _frame = (_frame + 1) % idleFrames.Length;
                else
                    _frame = 0;
                break;
            case State.Walk:
                if (walkFrames != null && walkFrames.Length > 0)
                    _frame = (_frame + 1) % walkFrames.Length;
                break;
            case State.Attack:
                _frame++;
                if (attackFrames == null || _frame >= attackFrames.Length)
                {
                    _state = State.Idle;
                    _frame = 0;
                }
                break;
            case State.Dead:
                if (deathFrames != null && deathFrames.Length > 0 && _frame < deathFrames.Length - 1)
                    _frame++;
                break;
        }

        ApplyFrame();
    }

    private float GetFps()
    {
        return _state switch
        {
            State.Idle => idleFps,
            State.Walk => walkFps,
            State.Attack => attackFps,
            State.Dead => deathFps,
            _ => walkFps
        };
    }

    private Sprite[] FramesForState()
    {
        return _state switch
        {
            State.Attack when attackFrames != null && attackFrames.Length > 0 => attackFrames,
            State.Dead when deathFrames != null && deathFrames.Length > 0 => deathFrames,
            State.Idle when idleFrames != null && idleFrames.Length > 0 => idleFrames,
            State.Idle => walkFrames,
            _ => walkFrames
        };
    }

    private void ApplyFrame()
    {
        if (spriteRenderer == null) return;
        var frames = FramesForState();
        if (frames == null || frames.Length == 0) return;
        var idx = Mathf.Clamp(_frame, 0, frames.Length - 1);
        spriteRenderer.sprite = frames[idx];
        UpdateFlip();
    }

    private void UpdateFlip()
    {
        if (!faceWithFlipX || spriteRenderer == null) return;
        if (Mathf.Abs(_lastFacing.x) < 0.001f) return;
        spriteRenderer.flipX = _lastFacing.x < 0f;
    }
}
