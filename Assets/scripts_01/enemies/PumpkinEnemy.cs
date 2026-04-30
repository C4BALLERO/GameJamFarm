using UnityEngine;

/// <summary>
/// Calabaza corrupta cuerpo a cuerpo (contact damage con cooldown).
/// </summary>
[DisallowMultipleComponent]
public sealed class PumpkinEnemy : EnemyBase
{
    [Header("Contact Damage")]
    [SerializeField] private float contactDamageInterval = 0.72f;
    private float _nextContactDamageAt;

    public override void PerformAttack(Transform target)
    {
        // Contact damage handled in OnCollisionStay2D.
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsDead) return;
        if (!collision.collider.TryGetComponent<IDamageable>(out var dmg)) return;
        if (dmg.IsDead) return;
        if (dmg is EnemyBase) return;
        if (dmg is not PlayerHealth && dmg is not AnimalBase) return;
        if (Time.time < _nextContactDamageAt) return;

        _nextContactDamageAt = Time.time + Mathf.Max(0.1f, contactDamageInterval);
        var dir = ((Vector2)dmg.Transform.position - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();
        dmg.TakeDamage(damage, dir * knockbackForce);
    }
}

