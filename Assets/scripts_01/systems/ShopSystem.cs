using UnityEngine;

public sealed class ShopSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private Transform animalSpawnRoot;

    [Header("Animal Prefabs")]
    [SerializeField] private GameObject cowPrefab;
    [SerializeField] private GameObject chickenPrefab;
    [SerializeField] private GameObject pigPrefab;

    [Header("Buy Prices (Essence)")]
    [SerializeField] private int cowCost = 3;
    [SerializeField] private int chickenCost = 2;
    [SerializeField] private int pigCost = 4;

    [Header("Sell Rates")]
    [SerializeField] private int meatToEssenceRate = 5;
    [SerializeField] private int bloodToEssenceRate = 5;

    public void Bind(InventorySystem inv) => inventory = inv;

    public bool BuyCow() => BuyAnimal(cowPrefab, cowCost);
    public bool BuyChicken() => BuyAnimal(chickenPrefab, chickenCost);
    public bool BuyPig() => BuyAnimal(pigPrefab, pigCost);

    public bool SellMeatForEssence()
    {
        if (inventory == null) return false;
        if (!inventory.Remove(ResourceType.Meat, meatToEssenceRate)) return false;
        inventory.Add(ResourceType.Essence, 1);
        return true;
    }

    public bool SellBloodForEssence()
    {
        if (inventory == null) return false;
        if (!inventory.Remove(ResourceType.Blood, bloodToEssenceRate)) return false;
        inventory.Add(ResourceType.Essence, 1);
        return true;
    }

    private bool BuyAnimal(GameObject prefab, int essenceCost)
    {
        if (prefab == null || inventory == null) return false;
        if (!inventory.Remove(ResourceType.Essence, essenceCost)) return false;

        var root = animalSpawnRoot != null ? animalSpawnRoot : transform;
        var pos = root.position + (Vector3)Random.insideUnitCircle * 1.5f;
        var go = Instantiate(prefab, pos, Quaternion.identity, root);

        if (go.TryGetComponent<ResourceGenerator>(out var gen))
        {
            gen.Init(inventory);
        }

        return true;
    }
}

