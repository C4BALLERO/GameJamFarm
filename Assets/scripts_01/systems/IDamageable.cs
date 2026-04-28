using UnityEngine;

/// <summary>
/// Interface for any entity that can take damage in the game.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Apply damage to this entity.
    /// </summary>
    void TakeDamage(int amount, Vector2 knockback);

    /// <summary>Current hit points.</summary>
    int GetHealth();

    /// <summary>Maximum hit points.</summary>
    int GetMaxHealth();

    /// <summary>
    /// Check if entity is dead
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// Get entity transform
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// Check if entity is alive
    /// </summary>
    bool IsAlive() => !IsDead;
}

