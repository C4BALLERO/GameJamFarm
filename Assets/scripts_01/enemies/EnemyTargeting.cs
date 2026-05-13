using UnityEngine;

/// <summary>
/// Prioridad: vallas vivas del corral → granero → vida del corral → jugador → animales (solo si no están protegidos).
/// </summary>
public static class EnemyTargeting
{
    /// <summary>Devuelve el transform a perseguir, o null.</summary>
    public static Transform ResolveNearestHostile(Vector2 origin)
    {
        Transform best = null;
        var bestSqr = float.MaxValue;

        foreach (var seg in Object.FindObjectsByType<CorralFenceSegment>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (seg == null || seg.IsDead)
                continue;
            var d = ((Vector2)seg.transform.position - origin).sqrMagnitude;
            if (d < bestSqr)
            {
                best = seg.transform;
                bestSqr = d;
            }
        }

        if (best != null)
            return best;

        foreach (var barn in Object.FindObjectsByType<BarnHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (barn == null || barn.IsDead)
                continue;
            var d = ((Vector2)barn.transform.position - origin).sqrMagnitude;
            if (d < bestSqr)
            {
                best = barn.transform;
                bestSqr = d;
            }
        }

        foreach (var ch in Object.FindObjectsByType<CorralHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (ch == null || ch.IsDestroyed)
                continue;
            var d = ((Vector2)ch.transform.position - origin).sqrMagnitude;
            if (d < bestSqr)
            {
                best = ch.transform;
                bestSqr = d;
            }
        }

        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null && player.TryGetComponent<PlayerHealth>(out var ph) && ph != null && !ph.IsDead)
        {
            var d = ((Vector2)player.transform.position - origin).sqrMagnitude;
            if (d < bestSqr)
            {
                best = player.transform;
                bestSqr = d;
            }
        }

        foreach (var fa in Object.FindObjectsByType<FarmAnimal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (fa == null || fa.IsDead)
                continue;
            if (FarmAnimalCorralProtection.IsProtected(fa))
                continue;

            var d = ((Vector2)fa.transform.position - origin).sqrMagnitude;
            if (d < bestSqr)
            {
                best = fa.transform;
                bestSqr = d;
            }
        }

        return best;
    }
}
