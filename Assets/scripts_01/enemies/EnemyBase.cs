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

    [Header("Cleanup")]
    [SerializeField] protected float deathDestroyDelaySeconds = 1.05f;

    public event Action Died;
    public event Action Damaged;

    public bool IsDead { get; private set; }
    public Transform Transform => transform;
    public int CurrentHealth { get; private set; }

    public int GetHealth() => CurrentHealth;
    public int GetMaxHealth() => maxHealth;
    public int GetDamage() => damage;

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
            if (TryGetComponent<Animator>(out var anim))
                anim.SetTrigger("Death");
            if (TryGetComponent<EnemySpriteAnimator>(out var visual))
                visual.TriggerDeath();
            Died?.Invoke();
            Destroy(gameObject, Mathf.Max(0.15f, deathDestroyDelaySeconds));
        }
    }

    public bool CanAttackNow() => !IsDead && Time.time >= nextAttackAt;

    public void ConsumeAttackCooldown()
    {
        nextAttackAt = Time.time + Mathf.Max(0.1f, attackCooldown);
    }

    public abstract void PerformAttack(Transform target);

    /// <summary>
    /// Apply a one-time difficulty scale to this enemy instance.
    /// Used by spawn systems so each new night can produce stronger enemies.
    /// </summary>
    public void ApplyDifficultyScaling(float healthMultiplier, float damageMultiplier)
    {
        if (IsDead)
            return;

        var safeHealthMul = Mathf.Max(0.1f, healthMultiplier);
        var safeDamageMul = Mathf.Max(0.1f, damageMultiplier);

        var previousMax = Mathf.Max(1, maxHealth);
        var scaledMax = Mathf.Max(1, Mathf.RoundToInt(previousMax * safeHealthMul));
        if (scaledMax != previousMax)
        {
            var gained = Mathf.Max(0, scaledMax - previousMax);
            maxHealth = scaledMax;
            CurrentHealth = Mathf.Clamp(CurrentHealth + gained, 1, maxHealth);
        }

        damage = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, damage) * safeDamageMul));
    }
}

