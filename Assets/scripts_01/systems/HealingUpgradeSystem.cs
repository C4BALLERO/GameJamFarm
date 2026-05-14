/// <summary>
/// Política de balance para restaurar vida en la tienda del granero: precio fijo por compra
/// (no escala al infinito). El valor efectivo lo aplica <see cref="ShopSystem"/> con su campo serializado.
/// </summary>
public static class HealingUpgradeSystem
{
    /// <summary>Valor por defecto recomendado en monedas (una compra = una curación).</summary>
    public const int DefaultFixedHealCoinCost = 50;
}
