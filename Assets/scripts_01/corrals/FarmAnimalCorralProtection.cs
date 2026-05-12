using UnityEngine;

/// <summary>
/// Mientras el corral tenga <see cref="CorralHealth"/> intacto, los <see cref="AnimalBase"/>
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

        var health = zone.GetComponent<CorralHealth>();
        if (health == null)
            return false;

        return !health.IsDestroyed;
    }
}
