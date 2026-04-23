using UnityEngine;

public sealed class PlantEnemy : EnemyBase
{
    // MVP: melee enemies hurt the player via collision contact.
    // Attack is mostly animation-driven in this simple version.
    public override void PerformAttack(Transform target)
    {
        // Nothing here: contact damage is handled in OnCollisionStay2D.
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsDead) return;
        if (!collision.collider.TryGetComponent<IDamageable>(out var dmg)) return;
        if (dmg.IsDead) return;

        var dir = ((Vector2)dmg.Transform.position - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();
        dmg.TakeDamage(damage, dir * knockbackForce);
    }
}

