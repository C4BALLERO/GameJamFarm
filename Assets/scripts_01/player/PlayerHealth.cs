using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float knockbackStopSeconds = 0.12f;

    public event Action<int, int> HealthChanged; // current, max
    public event Action Damaged;
    public event Action Died;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public Transform Transform => transform;

    private float _stopAt;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Update()
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
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

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
        }
    }
}

