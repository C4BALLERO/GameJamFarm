using UnityEngine;

/// <summary>
/// Mejoras por corral (almacén, vida del corral, velocidad de producción) pagadas con monedas.
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralUpgradeSystem : MonoBehaviour
{
    public static CorralUpgradeSystem Instance { get; private set; }

    [Header("Por nivel y tipo de corral")]
    [SerializeField] private int extraStorageSlotsPerLevel = 4;
    [SerializeField] private int extraCorralMaxHpPerLevel = 10;
    [Tooltip("Cada nivel reduce el intervalo de producción en esta fracción (0.08 = -8%).")]
    [SerializeField] [Range(0.02f, 0.25f)] private float productionIntervalCutPerLevel = 0.07f;

    [Header("Costes base (monedas) por compra de nivel")]
    [SerializeField] private int cowStorageUpgradeCost = 55;
    [SerializeField] private int chickenStorageUpgradeCost = 48;
    [SerializeField] private int pigStorageUpgradeCost = 52;
    [SerializeField] private int cowHealthUpgradeCost = 40;
    [SerializeField] private int chickenHealthUpgradeCost = 38;
    [SerializeField] private int pigHealthUpgradeCost = 42;
    [SerializeField] private int cowProductionUpgradeCost = 60;
    [SerializeField] private int chickenProductionUpgradeCost = 45;
    [SerializeField] private int pigProductionUpgradeCost = 50;

    [SerializeField] [Range(0f, 2f)] private float costGrowthPercent = 0.18f;

    public event System.Action UpgradesChanged;

    private int _cowS, _chickenS, _pigS;
    private int _cowH, _chickenH, _pigH;
    private int _cowP, _chickenP, _pigP;

    private int _runtimeCowStorageCost, _runtimeChickenStorageCost, _runtimePigStorageCost;
    private int _runtimeCowHealthCost, _runtimeChickenHealthCost, _runtimePigHealthCost;
    private int _runtimeCowProdCost, _runtimeChickenProdCost, _runtimePigProdCost;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResetRuntimeCosts();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ResetRuntimeCosts()
    {
        _runtimeCowStorageCost = cowStorageUpgradeCost;
        _runtimeChickenStorageCost = chickenStorageUpgradeCost;
        _runtimePigStorageCost = pigStorageUpgradeCost;
        _runtimeCowHealthCost = cowHealthUpgradeCost;
        _runtimeChickenHealthCost = chickenHealthUpgradeCost;
        _runtimePigHealthCost = pigHealthUpgradeCost;
        _runtimeCowProdCost = cowProductionUpgradeCost;
        _runtimeChickenProdCost = chickenProductionUpgradeCost;
        _runtimePigProdCost = pigProductionUpgradeCost;
    }

    public int GetExtraStorageSlots(FarmAnimalKind kind) =>
        Mathf.Max(0, GetStorageLevel(kind)) * Mathf.Max(0, extraStorageSlotsPerLevel);

    public int GetExtraCorralMaxHealth(FarmAnimalKind kind) =>
        Mathf.Max(0, GetHealthLevel(kind)) * Mathf.Max(0, extraCorralMaxHpPerLevel);

    /// <summary>Multiplicador del intervalo de ticks (&lt;1 = más rápido).</summary>
    public float GetProductionIntervalMultiplier(FarmAnimalKind kind)
    {
        var lv = GetProductionLevel(kind);
        if (lv <= 0)
            return 1f;
        var cut = Mathf.Clamp(productionIntervalCutPerLevel, 0.02f, 0.25f);
        return Mathf.Pow(1f - cut, lv);
    }

    public int GetStorageUpgradeCoinCost(FarmAnimalKind kind) => Mathf.Max(0, GetStorageRuntimeCost(kind));

    public int GetHealthUpgradeCoinCost(FarmAnimalKind kind) => Mathf.Max(0, GetHealthRuntimeCost(kind));

    public int GetProductionUpgradeCoinCost(FarmAnimalKind kind) => Mathf.Max(0, GetProductionRuntimeCost(kind));

    private int GetStorageLevel(FarmAnimalKind kind) => kind switch
    {
        FarmAnimalKind.Cow => _cowS,
        FarmAnimalKind.Chicken => _chickenS,
        FarmAnimalKind.Pig => _pigS,
        _ => 0
    };

    private int GetHealthLevel(FarmAnimalKind kind) => kind switch
    {
        FarmAnimalKind.Cow => _cowH,
        FarmAnimalKind.Chicken => _chickenH,
        FarmAnimalKind.Pig => _pigH,
        _ => 0
    };

    private int GetProductionLevel(FarmAnimalKind kind) => kind switch
    {
        FarmAnimalKind.Cow => _cowP,
        FarmAnimalKind.Chicken => _chickenP,
        FarmAnimalKind.Pig => _pigP,
        _ => 0
    };

    private int GetStorageRuntimeCost(FarmAnimalKind kind) => kind switch
    {
        FarmAnimalKind.Cow => _runtimeCowStorageCost,
        FarmAnimalKind.Chicken => _runtimeChickenStorageCost,
        FarmAnimalKind.Pig => _runtimePigStorageCost,
        _ => 0
    };

    private int GetHealthRuntimeCost(FarmAnimalKind kind) => kind switch
    {
        FarmAnimalKind.Cow => _runtimeCowHealthCost,
        FarmAnimalKind.Chicken => _runtimeChickenHealthCost,
        FarmAnimalKind.Pig => _runtimePigHealthCost,
        _ => 0
    };

    private int GetProductionRuntimeCost(FarmAnimalKind kind) => kind switch
    {
        FarmAnimalKind.Cow => _runtimeCowProdCost,
        FarmAnimalKind.Chicken => _runtimeChickenProdCost,
        FarmAnimalKind.Pig => _runtimePigProdCost,
        _ => 0
    };

    private void GrowRuntimeCost(ref int cost)
    {
        cost = Mathf.Max(cost + 1, Mathf.CeilToInt(cost * (1f + Mathf.Max(0f, costGrowthPercent))));
    }

    private void IncrementStorageLevel(FarmAnimalKind kind)
    {
        switch (kind)
        {
            case FarmAnimalKind.Cow: _cowS++; break;
            case FarmAnimalKind.Chicken: _chickenS++; break;
            default: _pigS++; break;
        }
    }

    private void IncrementHealthLevel(FarmAnimalKind kind)
    {
        switch (kind)
        {
            case FarmAnimalKind.Cow: _cowH++; break;
            case FarmAnimalKind.Chicken: _chickenH++; break;
            default: _pigH++; break;
        }
    }

    private void IncrementProductionLevel(FarmAnimalKind kind)
    {
        switch (kind)
        {
            case FarmAnimalKind.Cow: _cowP++; break;
            case FarmAnimalKind.Chicken: _chickenP++; break;
            default: _pigP++; break;
        }
    }

    private void GrowStorageRuntimeCost(FarmAnimalKind kind)
    {
        switch (kind)
        {
            case FarmAnimalKind.Cow: GrowRuntimeCost(ref _runtimeCowStorageCost); break;
            case FarmAnimalKind.Chicken: GrowRuntimeCost(ref _runtimeChickenStorageCost); break;
            default: GrowRuntimeCost(ref _runtimePigStorageCost); break;
        }
    }

    private void GrowHealthRuntimeCost(FarmAnimalKind kind)
    {
        switch (kind)
        {
            case FarmAnimalKind.Cow: GrowRuntimeCost(ref _runtimeCowHealthCost); break;
            case FarmAnimalKind.Chicken: GrowRuntimeCost(ref _runtimeChickenHealthCost); break;
            default: GrowRuntimeCost(ref _runtimePigHealthCost); break;
        }
    }

    private void GrowProductionRuntimeCost(FarmAnimalKind kind)
    {
        switch (kind)
        {
            case FarmAnimalKind.Cow: GrowRuntimeCost(ref _runtimeCowProdCost); break;
            case FarmAnimalKind.Chicken: GrowRuntimeCost(ref _runtimeChickenProdCost); break;
            default: GrowRuntimeCost(ref _runtimePigProdCost); break;
        }
    }

    public bool TryBuyStorageUpgrade(FarmAnimalKind kind, InventorySystem inventory)
    {
        if (inventory == null) return false;
        var raw = GetStorageUpgradeCoinCost(kind);
        var cost = BarnUpgradeSystem.Instance != null
            ? BarnUpgradeSystem.Instance.GetDiscountedShopPrice(raw)
            : raw;
        if (!inventory.Remove(ResourceType.Coin, cost))
            return false;

        IncrementStorageLevel(kind);
        GrowStorageRuntimeCost(kind);
        NotifyStorage(kind);
        UpgradesChanged?.Invoke();
        return true;
    }

    public bool TryBuyCorralHealthUpgrade(FarmAnimalKind kind, InventorySystem inventory)
    {
        if (inventory == null) return false;
        var raw = GetHealthUpgradeCoinCost(kind);
        var cost = BarnUpgradeSystem.Instance != null
            ? BarnUpgradeSystem.Instance.GetDiscountedShopPrice(raw)
            : raw;
        if (!inventory.Remove(ResourceType.Coin, cost))
            return false;

        IncrementHealthLevel(kind);
        GrowHealthRuntimeCost(kind);
        foreach (var h in UnityEngine.Object.FindObjectsByType<CorralHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null) continue;
            var z = h.GetComponent<CorralZone>();
            if (z != null && z.AllowedKind == kind)
                h.RecalculateMaxHealth();
        }

        UpgradesChanged?.Invoke();
        return true;
    }

    public bool TryBuyProductionUpgrade(FarmAnimalKind kind, InventorySystem inventory)
    {
        if (inventory == null) return false;
        var raw = GetProductionUpgradeCoinCost(kind);
        var cost = BarnUpgradeSystem.Instance != null
            ? BarnUpgradeSystem.Instance.GetDiscountedShopPrice(raw)
            : raw;
        if (!inventory.Remove(ResourceType.Coin, cost))
            return false;

        IncrementProductionLevel(kind);
        GrowProductionRuntimeCost(kind);
        UpgradesChanged?.Invoke();
        return true;
    }

    private void NotifyStorage(FarmAnimalKind kind)
    {
        foreach (var s in UnityEngine.Object.FindObjectsByType<CorralStorage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (s == null) continue;
            var z = s.GetComponent<CorralZone>();
            if (z != null && z.AllowedKind == kind)
                s.NotifyMaxCapacityMayHaveChanged();
        }
    }
}
