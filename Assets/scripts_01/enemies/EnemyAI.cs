using UnityEngine;

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
    [SerializeField] private Transform target;

    [Header("Ranges")]
    [SerializeField] private float aggroRange = 6f;
    [SerializeField] private float attackRange = 1.1f;

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

    public void SetTarget(Transform t) => target = t;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponent<EnemyBase>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteAnimator == null) spriteAnimator = GetComponent<EnemySpriteAnimator>();
    }

    private void Reset()
    {
        enemy = GetComponent<EnemyBase>();
        animator = GetComponentInChildren<Animator>();
        spriteAnimator = GetComponent<EnemySpriteAnimator>();
    }

    private void Update()
    {
        if (enemy == null || enemy.IsDead) return;
        if (target == null) return;

        var toTarget = (Vector2)target.position - (Vector2)transform.position;
        var dist = toTarget.magnitude;

        if (dist <= attackRange) _state = State.Attack;
        else if (dist <= aggroRange) _state = State.Chase;
        else _state = State.Idle;

        if (dist > 0.001f) _facing = toTarget.normalized;

        if (animator != null)
        {
            animator.SetFloat(moveX, _facing.x);
            animator.SetFloat(moveY, _facing.y);
            animator.SetBool(isMoving, _state == State.Chase);
        }
        if (spriteAnimator != null)
            spriteAnimator.SetMoveState(_facing, _state == State.Chase);

        if (_state == State.Attack && enemy.CanAttackNow())
        {
            enemy.ConsumeAttackCooldown();
            if (animator != null) animator.SetTrigger(triggerAttack);
            if (spriteAnimator != null) spriteAnimator.TriggerAttack();
            enemy.PerformAttack(target);
        }
    }

    private void FixedUpdate()
    {
        if (enemy == null || enemy.IsDead) return;
        if (!TryGetComponent<Rigidbody2D>(out var rb)) return;

        if (target == null || _state != State.Chase)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = _facing * moveSpeed;
    }
}

