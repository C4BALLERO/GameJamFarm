using UnityEngine;

public interface IDamageable
{
    bool IsDead { get; }
    Transform Transform { get; }
    void TakeDamage(int amount, Vector2 knockback);
}

