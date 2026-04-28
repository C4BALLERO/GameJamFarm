using UnityEngine;

/// <summary>
/// Melee corrupted plant: damages the player on sustained contact with a cooldown so <see cref="OnCollisionStay2D"/> does not drain HP every physics frame.
/// </summary>
public sealed class PlantEnemy : EnemyBase
{
    [Header("Contact Damage")]
    [SerializeField] private float contactDamageInterval = 0.65f;

    private float _nextContactDamageAt;

    public override void PerformAttack(Transform target)
    {
        // Contact damage is handled in OnCollisionStay2D; attack state drives movement/animation timing.
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsDead) return;
        if (!collision.collider.TryGetComponent<IDamageable>(out var dmg)) return;
        if (dmg.IsDead) return;
        if (dmg is EnemyBase) return; // no friendly fire between enemies
        if (dmg is not PlayerHealth && dmg is not AnimalBase) return;
        if (Time.time < _nextContactDamageAt) return;

        _nextContactDamageAt = Time.time + Mathf.Max(0.1f, contactDamageInterval);

        var dir = ((Vector2)dmg.Transform.position - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();
        dmg.TakeDamage(damage, dir * knockbackForce);
    }
}


