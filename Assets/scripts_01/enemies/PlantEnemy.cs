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
        // Contact damage is handled in OnCollisionStay2D / OnTriggerStay2D; attack state drives movement/animation timing.
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryApplyContactDamage(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyContactDamage(other);
    }

    private void TryApplyContactDamage(Collider2D hit)
    {
        if (IsDead) return;
        if (!DamageableResolver.TryResolve(hit, out var dmg))
            return;
        if (dmg.IsDead) return;
        if (dmg is EnemyBase) return;
        if (dmg is AnimalBase ab && FarmAnimalCorralProtection.IsProtected(ab)) return;
        if (dmg is not PlayerHealth && dmg is not AnimalBase && dmg is not BarnHealth && dmg is not CorralHealth &&
            dmg is not CorralDamageRelay && dmg is not CorralFenceSegment)
            return;
        if (Time.time < _nextContactDamageAt) return;

        _nextContactDamageAt = Time.time + Mathf.Max(0.1f, contactDamageInterval);

        var dir = ((Vector2)dmg.Transform.position - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();
        dmg.TakeDamage(damage, dir * knockbackForce);
    }
}
