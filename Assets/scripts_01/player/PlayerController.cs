using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private Rigidbody2D rb;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerSpriteAnimator spriteAnimator;
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
        spriteAnimator = GetComponent<PlayerSpriteAnimator>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
        if (health == null) health = GetComponent<PlayerHealth>();
        if (spriteAnimator == null) spriteAnimator = GetComponent<PlayerSpriteAnimator>();

        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            health.Revived += OnRevived;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            health.Revived -= OnRevived;
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

        _input = ReadMoveInput();
        _input = Vector2.ClampMagnitude(_input, 1f);

        if (_input.sqrMagnitude > 0.001f) _facing = _input;

        if (animator != null)
        {
            animator.SetFloat(moveX, _facing.x);
            animator.SetFloat(moveY, _facing.y);
            animator.SetBool(isMoving, _input.sqrMagnitude > 0.001f);
        }

        if (spriteAnimator != null)
            spriteAnimator.SetMovement(_facing, _input.sqrMagnitude > 0.001f);

        if (WasAttackPressed())
        {
            if (combat != null && combat.TryAttack(_facing))
            {
                if (animator != null) animator.SetTrigger(triggerAttack);
                if (spriteAnimator != null) spriteAnimator.TriggerAttack();
            }
        }
    }

    private static Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        var v = Vector2.zero;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
        return v;
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }

    private static bool WasAttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        var m = Mouse.current;
        return (k != null && k.spaceKey.wasPressedThisFrame) ||
               (m != null && m.leftButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
#endif
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
        if (spriteAnimator != null) spriteAnimator.TriggerDeath();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void OnRevived()
    {
        if (spriteAnimator != null) spriteAnimator.TriggerRevive();
    }
}

