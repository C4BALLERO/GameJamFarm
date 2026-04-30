using UnityEngine;

public sealed class RangedPlant : EnemyBase
{
    [Header("Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private int projectileDamageOverride = 1;
    [SerializeField] private float minProjectileAttackInterval = 1.25f;
    [SerializeField] private LayerMask hitLayers;
    private float _nextProjectileAt;

    public override void PerformAttack(Transform target)
    {
        if (projectilePrefab == null || target == null) return;
        if (Time.time < _nextProjectileAt) return;
        _nextProjectileAt = Time.time + Mathf.Max(0.2f, minProjectileAttackInterval);

        var origin = firePoint != null ? firePoint.position : transform.position;
        var toTarget = (Vector2)target.position - (Vector2)origin;
        var dir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector2.down;

        var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var finalDamage = projectileDamageOverride > 0 ? projectileDamageOverride : damage;
        proj.Fire(dir * projectileSpeed, finalDamage, knockbackForce, hitLayers);
    }
}

