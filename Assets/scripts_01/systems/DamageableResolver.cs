using UnityEngine;

/// <summary>
/// Resuelve <see cref="IDamageable"/> en el collider o en padres (p. ej. hitbox hijo + vida en la raíz).
/// </summary>
public static class DamageableResolver
{
    public static bool TryResolve(Collider2D hit, out IDamageable damageable)
    {
        damageable = null;
        if (hit == null)
            return false;

        damageable = hit.GetComponent<IDamageable>();
        if (damageable != null)
            return true;

        damageable = hit.GetComponentInParent<IDamageable>();
        return damageable != null;
    }
}
