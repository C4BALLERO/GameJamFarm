using UnityEngine;

/// <summary>
/// Mientras el corral tenga valla viva o <see cref="CorralHealth"/> intacto, los <see cref="AnimalBase"/>
/// bajo ese <see cref="CorralZone"/> no reciben daño de contacto enemigo.
/// </summary>
public static class FarmAnimalCorralProtection
{
    public static bool IsProtected(AnimalBase animal)
    {
        if (animal == null)
            return false;

        var zone = animal.GetComponentInParent<CorralZone>();
        if (zone == null)
            return false;

        if (HasLivingFence(zone))
            return true;

        var health = zone.GetComponent<CorralHealth>();
        return health != null && !health.IsDestroyed;
    }

    private static bool HasLivingFence(CorralZone zone)
    {
        if (zone == null)
            return false;
        var fenceRoot = zone.transform.Find("CorralFenceRoot");
        if (fenceRoot == null)
            return false;
        foreach (var seg in fenceRoot.GetComponentsInChildren<CorralFenceSegment>(true))
        {
            if (seg != null && !seg.IsDead)
                return true;
        }

        return false;
    }
}
