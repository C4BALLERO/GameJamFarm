using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private Rigidbody2D rb;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveX = "MoveX";
    [SerializeField] private string moveY = "MoveY";
    [SerializeField] private string isMoving = "IsMoving";
    [SerializeField] private string triggerAttack = "Attack";
    [SerializeField] private string triggerHurt = "Hurt";
    [SerializeField] private string triggerDeath = "Death";

    [Header("Combat/Health")]
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerHealth health;

    private Vector2 _input;
    private Vector2 _facing = Vector2.down;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
        if (health == null) health = GetComponent<PlayerHealth>();

        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            _input = Vector2.zero;
            if (animator != null) animator.SetBool(isMoving, false);
            return;
        }

        _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        _input = Vector2.ClampMagnitude(_input, 1f);

        if (_input.sqrMagnitude > 0.001f) _facing = _input;

        if (animator != null)
        {
            animator.SetFloat(moveX, _facing.x);
            animator.SetFloat(moveY, _facing.y);
            animator.SetBool(isMoving, _input.sqrMagnitude > 0.001f);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (combat != null && combat.TryAttack(_facing))
                if (animator != null) animator.SetTrigger(triggerAttack);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        if (health != null && health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = _input * moveSpeed;
    }

    private void OnDamaged()
    {
        if (animator != null) animator.SetTrigger(triggerHurt);
    }

    private void OnDied()
    {
        if (animator != null) animator.SetTrigger(triggerDeath);
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}

