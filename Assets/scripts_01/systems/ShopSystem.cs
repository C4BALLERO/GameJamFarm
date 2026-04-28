using UnityEngine;

/// <summary>
/// Manages shop transactions for buying animals and tools.
/// Handles resource trading between player and shop.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShopSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private Transform animalSpawnRoot;

    [Header("Animal Prefabs")]
    [SerializeField] private GameObject cowPrefab;
    [SerializeField] private GameObject chickenPrefab;
    [SerializeField] private GameObject pigPrefab;

    [Header("Buy Prices (Gold)")]
    [SerializeField] private int cowCost = 50;
    [SerializeField] private int chickenCost = 30;
    [SerializeField] private int pigCost = 40;

    [Header("Tool Prices (Gold)")]
    [SerializeField] private int hoePrice = 100;
    [SerializeField] private int axePrice = 75;
    [SerializeField] private int pickaxePrice = 80;

    [Header("Sell resources (batch sizes)")]
    [SerializeField] private int milkSellBatch = 4;
    [SerializeField] private int milkGoldPerBatch = 7;
    [SerializeField] private int eggSellBatch = 6;
    [SerializeField] private int eggGoldPerBatch = 6;
    [SerializeField] private int meatSellBatch = 4;
    [SerializeField] private int meatGoldPerBatch = 10;
    [SerializeField] private int woodSellBatch = 10;
    [SerializeField] private int woodGoldPerBatch = 2;

    [Header("Sell animals")]
    [SerializeField] private float animalSellSearchRadius = 40f;

    private void OnValidate()
    {
        if (inventory == null)
            inventory = GetComponent<InventorySystem>();
    }

    public void Bind(InventorySystem inv) => inventory = inv;

    #region Animal Purchasing
    
    /// <summary>
    /// Buy a cow for the farm
    /// </summary>
    public bool BuyCow() => BuyAnimal(cowPrefab, cowCost, "Cow");

    /// <summary>
    /// Buy a chicken for the farm
    /// </summary>
    public bool BuyChicken() => BuyAnimal(chickenPrefab, chickenCost, "Chicken");

    /// <summary>
    /// Buy a pig for the farm
    /// </summary>
    public bool BuyPig() => BuyAnimal(pigPrefab, pigCost, "Pig");

    #endregion

    #region Tool Purchasing

    /// <summary>
    /// Buy a hoe for farming
    /// </summary>
    public bool BuyHoe() => BuyTool(hoePrice, "Hoe");

    /// <summary>
    /// Buy an axe for logging
    /// </summary>
    public bool BuyAxe() => BuyTool(axePrice, "Axe");

    /// <summary>
    /// Buy a pickaxe for mining
    /// </summary>
    public bool BuyPickaxe() => BuyTool(pickaxePrice, "Pickaxe");

    #endregion

    #region Sell resources

    /// <summary>Sell a batch of milk for gold.</summary>
    public bool SellMilkBatch() => SellResource(ResourceType.Milk, milkSellBatch, milkGoldPerBatch, "Milk");

    /// <summary>Sell a batch of eggs for gold.</summary>
    public bool SellEggBatch() => SellResource(ResourceType.Egg, eggSellBatch, eggGoldPerBatch, "Egg");

    /// <summary>Sell a batch of meat for gold.</summary>
    public bool SellMeatBatch() => SellResource(ResourceType.Meat, meatSellBatch, meatGoldPerBatch, "Meat");

    /// <summary>Sell a batch of wood for gold.</summary>
    public bool SellWoodBatch() => SellResource(ResourceType.Wood, woodSellBatch, woodGoldPerBatch, "Wood");

    public bool SellCow() => SellAnimal(FarmAnimalKind.Cow, "Cow");
    public bool SellChicken() => SellAnimal(FarmAnimalKind.Chicken, "Chicken");
    public bool SellPig() => SellAnimal(FarmAnimalKind.Pig, "Pig");

    private bool SellResource(ResourceType type, int batchSize, int goldEarned, string label)
    {
        if (inventory == null || batchSize <= 0 || goldEarned <= 0) return false;
        if (!inventory.CanAfford(type, batchSize)) return false;
        if (!inventory.Remove(type, batchSize)) return false;

        inventory.Add(ResourceType.Gold, goldEarned);
        Debug.Log($"[Shop] Sold {batchSize} {label} for {goldEarned} gold");
        return true;
    }

    private bool SellAnimal(FarmAnimalKind kind, string label)
    {
        if (inventory == null) return false;

        var allAnimals = FindObjectsByType<FarmAnimal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        FarmAnimal best = null;
        var bestDist = float.MaxValue;
        var origin = animalSpawnRoot != null ? animalSpawnRoot.position : Vector3.zero;

        foreach (var a in allAnimals)
        {
            if (a == null || a.IsDead || a.Kind != kind) continue;
            var d = Vector3.SqrMagnitude(a.transform.position - origin);
            if (d < bestDist && d <= animalSellSearchRadius * animalSellSearchRadius)
            {
                best = a;
                bestDist = d;
            }
        }

        if (best == null) return false;
        var gold = best.SellGoldValue;
        inventory.Add(ResourceType.Gold, gold);
        Destroy(best.gameObject);
        Debug.Log($"[Shop] Sold {label} for {gold} gold");
        return true;
    }

    #endregion

    private bool BuyAnimal(GameObject prefab, int goldCost, string animalName)
    {
        if (prefab == null || inventory == null) 
            return false;

        if (!inventory.CanAfford(ResourceType.Gold, goldCost))
        {
            Debug.LogWarning($"[Shop] Not enough gold to buy {animalName}. Need: {goldCost}, Have: {inventory.Get(ResourceType.Gold)}");
            return false;
        }

        if (!inventory.Remove(ResourceType.Gold, goldCost))
            return false;

        var root = animalSpawnRoot != null ? animalSpawnRoot : transform;
        var pos = root.position + (Vector3)Random.insideUnitCircle * 1.5f;
        var go = Instantiate(prefab, pos, Quaternion.identity, root);

        if (go.TryGetComponent<ResourceGenerator>(out var gen))
            gen.Init(inventory);

        Debug.Log($"[Shop] Purchased {animalName} for {goldCost} gold");
        return true;
    }

    private bool BuyTool(int goldCost, string toolName)
    {
        if (inventory == null)
            return false;

        if (!inventory.CanAfford(ResourceType.Gold, goldCost))
            return false;

        if (!inventory.Remove(ResourceType.Gold, goldCost))
            return false;

        Debug.Log($"[Shop] Purchased {toolName} for {goldCost} gold");
        return true;
    }
}

