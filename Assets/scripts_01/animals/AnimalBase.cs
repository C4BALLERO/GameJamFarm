using System;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class AnimalBase : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 6;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float knockbackStopSeconds = 0.08f;

    public event Action Died;

    public bool IsDead { get; private set; }
    public Transform Transform => transform;
    public int CurrentHealth { get; private set; }

    private float _stopAt;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        if (rb == null) rb = GetComponent<Rigidbody2D>();
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
}

