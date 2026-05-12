using UnityEngine;

/// <summary>
/// Persigue el objetivo hostil más cercano (corral → granero → jugador → animales vulnerables).
/// La animación refleja la velocidad del rigidbody.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyAI : MonoBehaviour
{
    public enum State
    {
        Idle = 0,
        Chase = 1,
        Attack = 2
    }

    [Header("Refs")]
    [SerializeField] private EnemyBase enemy;
    [SerializeField] private Rigidbody2D rb;

    [Header("Targeting")]
    [SerializeField] private float retargetInterval = 0.22f;

    [Header("Ranges")]
    [SerializeField] private float attackRange = 1.15f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemySpriteAnimator spriteAnimator;
    [SerializeField] private string moveX = "MoveX";
    [SerializeField] private string moveY = "MoveY";
    [SerializeField] private string isMoving = "IsMoving";
    [SerializeField] private string triggerAttack = "Attack";

    private Vector2 _facing = Vector2.down;
    private State _state;

    private Transform _cachedTarget;
    private float _nextRetargetAt;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponent<EnemyBase>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteAnimator == null) spriteAnimator = GetComponent<EnemySpriteAnimator>();
    }

    private void Reset()
    {
        enemy = GetComponent<EnemyBase>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteAnimator = GetComponent<EnemySpriteAnimator>();
    }

    private void Update()
    {
        if (enemy == null || enemy.IsDead) return;

        var moving = rb != null && rb.linearVelocity.sqrMagnitude > 0.08f;

        if (animator != null)
        {
            animator.SetFloat(moveX, _facing.x);
            animator.SetFloat(moveY, _facing.y);
            animator.SetBool(isMoving, moving);
        }

        if (spriteAnimator != null)
            spriteAnimator.SetMoveState(_facing, moving);
    }

    private void FixedUpdate()
    {
        if (enemy == null || enemy.IsDead || rb == null)
            return;

        var target = ResolveNearestHostileTarget();
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            _state = State.Idle;
            return;
        }

        var toTarget = (Vector2)target.position - rb.position;
        var dist = toTarget.magnitude;

        _state = dist <= attackRange ? State.Attack : State.Chase;

        if (dist > 0.001f)
            _facing = toTarget.normalized;

        if (_state == State.Chase)
            rb.linearVelocity = _facing * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;

        if (_state == State.Attack && enemy.CanAttackNow())
        {
            enemy.ConsumeAttackCooldown();
            if (animator != null)
                animator.SetTrigger(triggerAttack);
            if (spriteAnimator != null)
                spriteAnimator.TriggerAttack();

            enemy.PerformAttack(target);
        }
    }

    private Transform ResolveNearestHostileTarget()
    {
        if (Time.time < _nextRetargetAt && _cachedTarget != null)
            return _cachedTarget;

        _nextRetargetAt = Time.time + Mathf.Max(0.05f, retargetInterval);

        var origin = rb != null ? rb.position : (Vector2)transform.position;
        _cachedTarget = EnemyTargeting.ResolveNearestHostile(origin);
        return _cachedTarget;
    }
}
