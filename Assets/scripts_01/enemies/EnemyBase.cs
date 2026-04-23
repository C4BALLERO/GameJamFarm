using System;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Core")]
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Animator animator;

    [Header("Health")]
    [SerializeField] protected int maxHealth = 4;
    [SerializeField] protected float knockbackStopSeconds = 0.08f;

    [Header("Attack")]
    [SerializeField] protected int damage = 1;
    [SerializeField] protected float knockbackForce = 2.5f;
    [SerializeField] protected float attackCooldown = 1.0f;

    public event Action Died;
    public event Action Damaged;

    public bool IsDead { get; private set; }
    public Transform Transform => transform;
    public int CurrentHealth { get; private set; }

    private float _stopAt;
    protected float nextAttackAt;

    protected virtual void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        if (rb == null) return;
        if (_stopAt <= 0f) return;
        if (Time.time < _stopAt) return;
        rb.linearVelocity = Vector2.zero;
        _stopAt = 0f;
    }

    public void TakeDamage(int amount, Vector2 knockback)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Damaged?.Invoke();

        if (rb != null && knockback != Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockback, ForceMode2D.Impulse);
            _stopAt = Time.time + knockbackStopSeconds;
        }

        if (CurrentHealth <= 0)
        {
            IsDead = true;
            Died?.Invoke();
            Destroy(gameObject, 0.15f);
        }
    }

    public bool CanAttackNow() => !IsDead && Time.time >= nextAttackAt;

    public void ConsumeAttackCooldown()
    {
        nextAttackAt = Time.time + Mathf.Max(0.1f, attackCooldown);
    }

    public abstract void PerformAttack(Transform target);
}

