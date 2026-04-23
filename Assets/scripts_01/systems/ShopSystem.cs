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

