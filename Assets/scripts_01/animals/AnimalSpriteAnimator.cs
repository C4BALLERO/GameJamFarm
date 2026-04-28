using UnityEngine;

/// <summary>
/// Farm animals: idle loop, walk when moving, death on <see cref="AnimalBase"/>.Died.
/// Optionally syncs Animator bool <c>IsMoving</c> when an <see cref="Animator"/> is present.
/// Expects 36-frame sprite sheets split in the Sprite Editor (Idle / Walk / Death clips assigned here).
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimalSpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D body;

    [Header("Animator (optional — assign controller generated via Tools menu)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animatorMovingBool = "IsMoving";
    [SerializeField] private string animatorDeathTrigger = "Death";

    [Header("Frames (tip: use Multiple sprite mode 6×6 = 36 frames)")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] deathFrames;

    [Header("Timing")]
    [SerializeField] private float idleFps = 6f;
    [SerializeField] private float walkFps = 8f;
    [SerializeField] private float deathFps = 10f;
    [SerializeField] private float movingVelocityThreshold = 0.08f;
    [SerializeField] private bool faceWithFlipX = true;

    private enum AnimState
    {
        Idle,
        Walk,
        Dead
    }

    private AnimState _state = AnimState.Idle;
    private int _frame;
    private float _nextFrameAt;
    private AnimalBase _animal;
    private Vector2 _lastFacing = Vector2.right;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (body == null)
            TryGetComponent(out body);
        TryGetComponent(out _animal);

        if (_animal != null)
            _animal.Died += OnAnimalDied;

        ApplyFrame();
    }

    private void OnDestroy()
    {
        if (_animal != null)
            _animal.Died -= OnAnimalDied;
    }

    private void OnAnimalDied()
    {
        _state = AnimState.Dead;
        _frame = 0;
        _nextFrameAt = 0f;

        if (animator != null && !string.IsNullOrEmpty(animatorDeathTrigger))
            animator.SetTrigger(animatorDeathTrigger);

        ApplyFrame();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen)
            return;

        if (_state == AnimState.Dead)
            TickDeath();
        else
            TickLiving();

        SyncAnimatorMovingFlag();
    }

    private void TickLiving()
    {
        var vel = ResolveVisualVelocity();
        var moving = vel.sqrMagnitude > movingVelocityThreshold * movingVelocityThreshold;
        _state = moving ? AnimState.Walk : AnimState.Idle;

        if (vel.sqrMagnitude > 0.0004f)
            _lastFacing = vel.normalized;

        var now = Time.time;
        if (now < _nextFrameAt)
            return;

        var fps = _state == AnimState.Walk ? walkFps : idleFps;
        _nextFrameAt = now + 1f / Mathf.Max(1f, fps);

        var frames = _state == AnimState.Walk ? walkFrames : idleFrames;
        if (frames == null || frames.Length == 0)
        {
            _frame = 0;
            ApplyFrame();
            return;
        }

        _frame = (_frame + 1) % frames.Length;
        ApplyFrame();
    }

    private void TickDeath()
    {
        var now = Time.time;
        if (now < _nextFrameAt)
            return;

        var fps = Mathf.Max(1f, deathFps);
        _nextFrameAt = now + 1f / fps;

        if (deathFrames == null || deathFrames.Length == 0)
            return;

        if (_frame < deathFrames.Length - 1)
            _frame++;

        ApplyFrame();
    }

    private void SyncAnimatorMovingFlag()
    {
        if (animator == null) return;
        if (_state == AnimState.Dead)
        {
            animator.SetBool(animatorMovingBool, false);
            return;
        }

        var vel = ResolveVisualVelocity();
        var moving = vel.sqrMagnitude > movingVelocityThreshold * movingVelocityThreshold;
        animator.SetBool(animatorMovingBool, moving);
    }

    /// <summary>Kinematic corral motors use MovePosition — velocity stays zero; prefer motor.LastVelocity.</summary>
    private Vector2 ResolveVisualVelocity()
    {
        if (TryGetComponent<FarmAnimalCorralMotor>(out var motor))
            return motor.LastVelocity;

        return body != null ? body.linearVelocity : Vector2.zero;
    }

    private void ApplyFrame()
    {
        if (spriteRenderer == null) return;

        Sprite[] frames;
        int idx;

        switch (_state)
        {
            case AnimState.Walk:
                frames = walkFrames != null && walkFrames.Length > 0 ? walkFrames : idleFrames;
                idx = Mathf.Clamp(_frame, 0, Mathf.Max(0, frames.Length - 1));
                break;
            case AnimState.Dead:
                frames = deathFrames != null && deathFrames.Length > 0 ? deathFrames : idleFrames;
                idx = Mathf.Clamp(_frame, 0, Mathf.Max(0, frames.Length - 1));
                break;
            default:
                frames = idleFrames != null && idleFrames.Length > 0 ? idleFrames : walkFrames;
                idx = Mathf.Clamp(_frame, 0, Mathf.Max(0, frames.Length - 1));
                break;
        }

        if (frames == null || frames.Length == 0)
            return;

        spriteRenderer.sprite = frames[idx];

        if (faceWithFlipX && Mathf.Abs(_lastFacing.x) > 0.001f)
            spriteRenderer.flipX = _lastFacing.x < 0f;
    }
}
