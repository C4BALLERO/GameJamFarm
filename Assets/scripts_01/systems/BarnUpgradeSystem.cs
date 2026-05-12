using System;
using UnityEngine;

/// <summary>
/// Nivel del granero: descuenta precios de compra y mejora ligeramente la venta de recursos.
/// </summary>
[DisallowMultipleComponent]
public sealed class BarnUpgradeSystem : MonoBehaviour
{
    public static BarnUpgradeSystem Instance { get; private set; }

    [Header("Nivel actual")]
    [SerializeField] [Min(0)] private int barnTier;

    [Header("Efectos por nivel")]
    [Tooltip("Reducción acumulada del coste en tienda (5% por nivel hasta el tope).")]
    [SerializeField] [Range(0.01f, 0.12f)] private float shopDiscountPerTier = 0.05f;
    [SerializeField] [Range(0.3f, 1f)] private float minShopPriceMultiplier = 0.72f;
    [Tooltip("Bonus acumulado a monedas ganadas al vender.")]
    [SerializeField] [Range(0.01f, 0.15f)] private float sellBonusPerTier = 0.04f;

    [Header("Coste en monedas para subir de nivel")]
    [SerializeField] private int baseUpgradeCost = 120;
    [SerializeField] [Range(0f, 2f)] private float upgradeCostGrowth = 0.35f;

    private int _runtimeUpgradeCost;

    public int Tier => Mathf.Max(0, barnTier);

    /// <summary>Multiplicador aplicado a precios de compra (&lt;=1).</summary>
    public float ShopPriceMultiplier =>
        Mathf.Max(minShopPriceMultiplier, 1f - Tier * Mathf.Max(0.01f, shopDiscountPerTier));

    /// <summary>Multiplicador aplicado al valor de venta (&gt;=1).</summary>
    public float SellRewardMultiplier => 1f + Tier * Mathf.Max(0f, sellBonusPerTier);

    public int GetDiscountedShopPrice(int baseCoinAmount) =>
        Mathf.Max(0, Mathf.RoundToInt(baseCoinAmount * ShopPriceMultiplier));

    public event Action TierChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _runtimeUpgradeCost = Mathf.Max(1, baseUpgradeCost);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetNextTierCoinCost() => Mathf.Max(1, _runtimeUpgradeCost);

    public bool TryBuyNextTier(InventorySystem inventory)
    {
        if (inventory == null)
            return false;

        var cost = _runtimeUpgradeCost;
        if (!inventory.Remove(ResourceType.Coin, cost))
            return false;

        barnTier++;
        _runtimeUpgradeCost = Mathf.Max(_runtimeUpgradeCost + 1,
            Mathf.CeilToInt(_runtimeUpgradeCost * (1f + Mathf.Max(0f, upgradeCostGrowth))));
        TierChanged?.Invoke();
        return true;
    }
}
