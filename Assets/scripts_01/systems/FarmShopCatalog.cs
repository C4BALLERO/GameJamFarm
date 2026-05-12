using UnityEngine;

/// <summary>
/// Textos de tienda (nombres, recurso producido, descripciones de power-ups). Opcional: créalo con
/// <i>Create → Dark Farm → Shop Catalog</i> y asígnalo en <see cref="ShopUI"/>.
/// </summary>
[CreateAssetMenu(fileName = "FarmShopCatalog", menuName = "Dark Farm/Shop Catalog", order = 0)]
public sealed class FarmShopCatalog : ScriptableObject
{
    [Header("Animales")]
    public string cowName = "Vaca";
    [TextArea(1, 2)] public string cowResourceLine = "Produce: leche (almacén del corral)";
    public string chickenName = "Gallina";
    [TextArea(1, 2)] public string chickenResourceLine = "Produce: huevos (almacén del corral)";
    public string pigName = "Cerdo";
    [TextArea(1, 2)] public string pigResourceLine = "Produce: carne (almacén del corral)";

    [Header("Power-ups (orden: prod, vida animal, daño jug., mov. jug., spawn, almacén corral)")]
    [TextArea(2, 4)] public string fasterGenerationDescription = "Los animales producen recursos algo más rápido en el corral.";
    [TextArea(2, 4)] public string animalHealthDescription = "Aumenta la vida máxima de los animales ya colocados.";
    [TextArea(2, 4)] public string playerDamageDescription = "Sube el daño cuerpo a cuerpo del jugador.";
    [TextArea(2, 4)] public string playerMoveDescription = "Aumenta la velocidad de movimiento del jugador.";
    [TextArea(2, 4)] public string spawnDelayDescription = "Los enemigos aparecen con un poco más de separación entre oleadas.";
    [TextArea(2, 4)] public string corralStorageDescription = "Aumenta el máximo de recursos que cabe en cada corral antes de recoger.";

    public string GetPowerUpDescription(int index)
    {
        return index switch
        {
            0 => fasterGenerationDescription,
            1 => animalHealthDescription,
            2 => playerDamageDescription,
            3 => playerMoveDescription,
            4 => spawnDelayDescription,
            5 => corralStorageDescription,
            _ => string.Empty
        };
    }
}
