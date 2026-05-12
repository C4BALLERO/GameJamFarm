using System;
using UnityEngine;

/// <summary>
/// Granero: compras con monedas, venta de recursos vía <see cref="SellSystem"/>, power-ups con precio progresivo.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShopSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private AnimalSpawner animalSpawner;
    [SerializeField] private PowerUpSystem powerUpSystem;

    [Header("Animal Prefabs")]
    [SerializeField] private GameObject cowPrefab;
    [SerializeField] private GameObject chickenPrefab;
    [SerializeField] private GameObject pigPrefab;

    [Header("Precios animales (monedas)")]
    [SerializeField] private int cowCoinPrice = 85;
    [SerializeField] private int chickenCoinPrice = 28;
    [SerializeField] private int pigCoinPrice = 55;

    [Header("Mejoras jugador (monedas)")]
    [SerializeField] private int attackUpgradeCoinBase = 42;
    [SerializeField] private int speedUpgradeCoinBase = 38;

    [Header("Power-Ups (monedas base por índice: prod, vida animal, daño jug, mov jug, spawn, almacén)")]
    [SerializeField] private int[] basePowerUpCoinCosts = { 36, 32, 48, 40, 52, 34 };

    [Header("Escalado precio Power-Ups (y mejoras de moneda)")]
    [SerializeField] [Range(0f, 3f)] private float powerUpCostGrowthPercent = 0.22f;
    [SerializeField] private int powerUpCostGrowthFlat = 1;

    [Header("Restaurar vida jugador (monedas)")]
    [SerializeField] private int playerHealCoinBase = 18;
    [SerializeField] private int healthRestoreAmount = 3;

    private int[] _runtimePowerUpCoins;
    private int _runtimeAttackCoin;
    private int _runtimeSpeedCoin;
    private int _runtimeHealCoin;

    private void Awake()
    {
        if (animalSpawner == null)
            animalSpawner = FindFirstObjectByType<AnimalSpawner>();
        if (powerUpSystem == null)
            powerUpSystem = FindFirstObjectByType<PowerUpSystem>();

        _runtimePowerUpCoins = basePowerUpCoinCosts != null && basePowerUpCoinCosts.Length > 0
            ? (int[])basePowerUpCoinCosts.Clone()
            : new int[6];
        if (_runtimePowerUpCoins.Length < 6)
        {
            var n = new int[6];
            for (var i = 0; i < 6; i++)
                n[i] = i < _runtimePowerUpCoins.Length ? _runtimePowerUpCoins[i] : 30;
            _runtimePowerUpCoins = n;
        }

        _runtimeAttackCoin = attackUpgradeCoinBase;
        _runtimeSpeedCoin = speedUpgradeCoinBase;
        _runtimeHealCoin = playerHealCoinBase;
    }

    private void OnValidate()
    {
        if (inventory == null)
            inventory = GetComponent<InventorySystem>();
    }

    public void Bind(InventorySystem inv) => inventory = inv;

    public void BindSpawner(AnimalSpawner spawner) => animalSpawner = spawner;

    public void BindPowerUps(PowerUpSystem system) => powerUpSystem = system;

    private int DiscountedShopCoins(int baseAmount)
    {
        return BarnUpgradeSystem.Instance != null
            ? BarnUpgradeSystem.Instance.GetDiscountedShopPrice(baseAmount)
            : Mathf.Max(0, baseAmount);
    }

    public int GetAnimalCoinPrice(FarmAnimalKind kind)
    {
        var raw = kind switch
        {
            FarmAnimalKind.Cow => Mathf.Max(0, cowCoinPrice),
            FarmAnimalKind.Chicken => Mathf.Max(0, chickenCoinPrice),
            FarmAnimalKind.Pig => Mathf.Max(0, pigCoinPrice),
            _ => 0
        };
        return DiscountedShopCoins(raw);
    }

    public int GetAttackUpgradeCoinCost() => DiscountedShopCoins(Mathf.Max(0, _runtimeAttackCoin));

    public int GetSpeedUpgradeCoinCost() => DiscountedShopCoins(Mathf.Max(0, _runtimeSpeedCoin));

    public int GetPowerUpCoinCost(int index)
    {
        if (_runtimePowerUpCoins == null || index < 0 || index >= _runtimePowerUpCoins.Length)
            return 0;
        return DiscountedShopCoins(Mathf.Max(0, _runtimePowerUpCoins[index]));
    }

    public int GetPlayerHealCoinCost() => DiscountedShopCoins(Mathf.Max(0, _runtimeHealCoin));

    /// <summary>Compatibilidad antigua: ya no se usan recursos para comprar animales.</summary>
    public ResourceCost[] GetPurchaseCosts(FarmAnimalKind kind) => Array.Empty<ResourceCost>();

    public ResourceCost[] GetPlayerHealthRestoreCosts() => Array.Empty<ResourceCost>();

    public ResourceCost[] GetAttackUpgradeCosts() => Array.Empty<ResourceCost>();

    public ResourceCost[] GetSpeedUpgradeCosts() => Array.Empty<ResourceCost>();

    public ResourceCost[] GetFasterGenerationCosts() => Array.Empty<ResourceCost>();

    public ResourceCost[] GetAnimalHealthCosts() => Array.Empty<ResourceCost>();

    public ResourceCost[] GetPlayerDamageBoostCosts() => Array.Empty<ResourceCost>();

    public ResourceCost[] GetPlayerMoveBoostCosts() => Array.Empty<ResourceCost>();

    public ResourceCost[] GetReducedSpawnDelayCosts() => Array.Empty<ResourceCost>();

    public GameObject GetAnimalPrefab(FarmAnimalKind kind)
    {
        return kind switch
        {
            FarmAnimalKind.Cow => cowPrefab,
            FarmAnimalKind.Chicken => chickenPrefab,
            FarmAnimalKind.Pig => pigPrefab,
            _ => null
        };
    }

    #region Animals

    public bool BuyCow() => TryBuyAnimal(FarmAnimalKind.Cow, cowPrefab, "Vaca");

    public bool BuyChicken() => TryBuyAnimal(FarmAnimalKind.Chicken, chickenPrefab, "Gallina");

    public bool BuyPig() => TryBuyAnimal(FarmAnimalKind.Pig, pigPrefab, "Cerdo");

    private bool TryBuyAnimal(FarmAnimalKind kind, GameObject prefab, string label)
    {
        if (prefab == null || inventory == null) return false;

        if (animalSpawner == null)
            animalSpawner = FindFirstObjectByType<AnimalSpawner>();

        if (animalSpawner == null)
        {
            Debug.LogError("[Shop] Falta AnimalSpawner en la escena.");
            return false;
        }

        var baseCost = kind switch
        {
            FarmAnimalKind.Cow => cowCoinPrice,
            FarmAnimalKind.Chicken => chickenCoinPrice,
            FarmAnimalKind.Pig => pigCoinPrice,
            _ => 0
        };
        var coinCost = DiscountedShopCoins(Mathf.Max(0, baseCost));

        if (!TrySpendCoins(coinCost))
        {
            Debug.LogWarning($"[Shop] No alcanzan monedas para comprar {label}.");
            return false;
        }

        if (animalSpawner.TrySpawnPurchasedAnimal(kind, prefab, inventory, out _))
        {
            Debug.Log($"[Shop] Compraste {label}.");
            return true;
        }

        RefundCoins(coinCost);
        Debug.LogWarning($"[Shop] No se pudo colocar {label} (corral lleno). Monedas devueltas.");
        return false;
    }

    #endregion

    #region PowerUps

    public bool BuyFasterGenerationPowerUp() =>
        TryBuyPowerUpCoin(0, "Generacion rapida", p => p.BuyResourceGenerationBoost());

    public bool BuyAnimalHealthPowerUp() =>
        TryBuyPowerUpCoin(1, "Vida animal", p => p.BuyAnimalHealthBoost());

    public bool BuyPlayerDamagePowerUp() =>
        TryBuyPowerUpCoin(2, "Danio jugador", p => p.BuyPlayerDamageBoost());

    public bool BuyPlayerMovePowerUp() =>
        TryBuyPowerUpCoin(3, "Movimiento jugador", p => p.BuyPlayerMoveBoost());

    public bool BuyReducedSpawnDelayPowerUp() =>
        TryBuyPowerUpCoin(4, "Reducir spawn enemigo", p => p.BuySpawnDelayReductionBoost());

    public bool BuyCorralStoragePowerUp() =>
        TryBuyPowerUpCoin(5, "Mas almacen en corral", p => p.BuyCorralStorageBoost());

    public bool BuyPlayerHealthRestore()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph == null)
        {
            Debug.LogWarning("[Shop] No hay PlayerHealth.");
            return false;
        }

        var cost = DiscountedShopCoins(_runtimeHealCoin);
        if (!TrySpendCoins(cost))
        {
            Debug.LogWarning("[Shop] No alcanzan monedas para restaurar vida.");
            return false;
        }

        ph.Heal(healthRestoreAmount);
        GrowCoinPriceInstance(ref _runtimeHealCoin);
        Debug.Log($"[Shop] Vida restaurada: +{healthRestoreAmount}.");
        return true;
    }

    private bool TryBuyPowerUpCoin(int index, string label, Action<PowerUpSystem> apply)
    {
        if (powerUpSystem == null)
            powerUpSystem = FindFirstObjectByType<PowerUpSystem>();
        if (powerUpSystem == null || apply == null)
            return false;

        if (index < 0 || index >= _runtimePowerUpCoins.Length)
            return false;

        var cost = DiscountedShopCoins(_runtimePowerUpCoins[index]);
        if (!TrySpendCoins(cost))
        {
            Debug.LogWarning($"[Shop] No alcanzan monedas para power-up: {label}.");
            return false;
        }

        apply(powerUpSystem);
        GrowCoinPriceInstance(ref _runtimePowerUpCoins[index]);
        Debug.Log($"[Shop] Power-up comprado: {label}.");
        return true;
    }

    private void GrowCoinPriceInstance(ref int price)
    {
        var grown = Mathf.CeilToInt(price * (1f + Mathf.Max(0f, powerUpCostGrowthPercent)));
        price = Mathf.Max(price + 1, grown + Mathf.Max(0, powerUpCostGrowthFlat));
    }

    #endregion

    #region Upgrades

    public bool BuyAttackUpgrade()
    {
        var pc = FindFirstObjectByType<PlayerCombat>();
        if (pc == null)
        {
            Debug.LogWarning("[Shop] No hay PlayerCombat.");
            return false;
        }

        var cost = DiscountedShopCoins(_runtimeAttackCoin);
        if (!TrySpendCoins(cost))
        {
            Debug.LogWarning("[Shop] No alcanzan monedas para mejorar ataque.");
            return false;
        }

        pc.IncrementAttackTier();
        GrowCoinPriceInstance(ref _runtimeAttackCoin);
        Debug.Log("[Shop] Ataque mejorado.");
        return true;
    }

    public bool BuySpeedUpgrade()
    {
        var pl = FindFirstObjectByType<PlayerController>();
        if (pl == null)
        {
            Debug.LogWarning("[Shop] No hay PlayerController.");
            return false;
        }

        var cost = DiscountedShopCoins(_runtimeSpeedCoin);
        if (!TrySpendCoins(cost))
        {
            Debug.LogWarning("[Shop] No alcanzan monedas para mejorar velocidad.");
            return false;
        }

        pl.IncrementSpeedTier();
        GrowCoinPriceInstance(ref _runtimeSpeedCoin);
        Debug.Log("[Shop] Velocidad mejorada.");
        return true;
    }

    #endregion

    #region Corral y granero (mejoras permanentes de escena)

    public int GetBarnTierUpgradeCoinCost() =>
        Mathf.Max(0, BarnUpgradeSystem.Instance != null ? BarnUpgradeSystem.Instance.GetNextTierCoinCost() : 0);

    public bool BuyBarnTierUpgrade() =>
        inventory != null && BarnUpgradeSystem.Instance != null && BarnUpgradeSystem.Instance.TryBuyNextTier(inventory);

    public bool BuyCorralStorageUpgrade(FarmAnimalKind kind) =>
        inventory != null && CorralUpgradeSystem.Instance != null &&
        CorralUpgradeSystem.Instance.TryBuyStorageUpgrade(kind, inventory);

    public bool BuyCorralHealthUpgrade(FarmAnimalKind kind) =>
        inventory != null && CorralUpgradeSystem.Instance != null &&
        CorralUpgradeSystem.Instance.TryBuyCorralHealthUpgrade(kind, inventory);

    public bool BuyCorralProductionUpgrade(FarmAnimalKind kind) =>
        inventory != null && CorralUpgradeSystem.Instance != null &&
        CorralUpgradeSystem.Instance.TryBuyProductionUpgrade(kind, inventory);

    public int GetCorralStorageUpgradeCoinCost(FarmAnimalKind kind) =>
        CorralUpgradeSystem.Instance != null
            ? DiscountedShopCoins(CorralUpgradeSystem.Instance.GetStorageUpgradeCoinCost(kind))
            : 0;

    public int GetCorralHealthUpgradeCoinCost(FarmAnimalKind kind) =>
        CorralUpgradeSystem.Instance != null
            ? DiscountedShopCoins(CorralUpgradeSystem.Instance.GetHealthUpgradeCoinCost(kind))
            : 0;

    public int GetCorralProductionUpgradeCoinCost(FarmAnimalKind kind) =>
        CorralUpgradeSystem.Instance != null
            ? DiscountedShopCoins(CorralUpgradeSystem.Instance.GetProductionUpgradeCoinCost(kind))
            : 0;

    #endregion

    private bool TrySpendCoins(int amount)
    {
        if (inventory == null) return false;
        if (amount <= 0) return true;
        return inventory.Remove(ResourceType.Coin, amount);
    }

    private void RefundCoins(int amount)
    {
        if (inventory == null || amount <= 0) return;
        inventory.Add(ResourceType.Coin, amount);
    }
}
