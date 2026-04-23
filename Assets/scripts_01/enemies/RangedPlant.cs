using UnityEngine;

public sealed class RangedPlant : EnemyBase
{
    [Header("Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private LayerMask hitLayers;

    public override void PerformAttack(Transform target)
    {
        if (projectilePrefab == null || target == null) return;

        var origin = firePoint != null ? firePoint.position : transform.position;
        var toTarget = (Vector2)target.position - (Vector2)origin;
        var dir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector2.down;

        var proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        proj.Fire(dir * projectileSpeed, damage, knockbackForce, hitLayers);
    }
}

