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

    /// <summary>
    /// Get current health
    /// </summary>
    int GetHealth();

    /// <summary>
    /// Get maximum health
    /// </summary>
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

