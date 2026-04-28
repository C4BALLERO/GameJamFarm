using System;
using UnityEngine;

/// <summary>
/// Granero: compra animales y mejoras de ataque/velocidad solo con Leche, Huevos y Carne.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShopSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private AnimalSpawner animalSpawner;

    [Header("Animal Prefabs")]
    [SerializeField] private GameObject cowPrefab;
    [SerializeField] private GameObject chickenPrefab;
    [SerializeField] private GameObject pigPrefab;

    [Header("Costes animales (solo Leche / Huevos / Carne)")]
    [SerializeField] private ResourceCost[] chickenPurchaseCosts =
    {
        new ResourceCost { type = ResourceType.Egg, amount = 6 },
        new ResourceCost { type = ResourceType.Meat, amount = 8 },
        new ResourceCost { type = ResourceType.Milk, amount = 3 }
    };

    [SerializeField] private ResourceCost[] pigPurchaseCosts =
    {
        new ResourceCost { type = ResourceType.Meat, amount = 10 },
        new ResourceCost { type = ResourceType.Milk, amount = 8 },
        new ResourceCost { type = ResourceType.Egg, amount = 5 }
    };

    [SerializeField] private ResourceCost[] cowPurchaseCosts =
    {
        new ResourceCost { type = ResourceType.Milk, amount = 14 },
        new ResourceCost { type = ResourceType.Egg, amount = 12 },
        new ResourceCost { type = ResourceType.Meat, amount = 18 }
    };

    [Header("Mejoras jugador")]
    [SerializeField] private ResourceCost[] attackUpgradeCosts =
    {
        new ResourceCost { type = ResourceType.Milk, amount = 10 },
        new ResourceCost { type = ResourceType.Egg, amount = 10 },
        new ResourceCost { type = ResourceType.Meat, amount = 8 }
    };

    [SerializeField] private ResourceCost[] speedUpgradeCosts =
    {
        new ResourceCost { type = ResourceType.Egg, amount = 14 },
        new ResourceCost { type = ResourceType.Milk, amount = 9 },
        new ResourceCost { type = ResourceType.Meat, amount = 10 }
    };

    private void Awake()
    {
        if (animalSpawner == null)
            animalSpawner = FindFirstObjectByType<AnimalSpawner>();
    }

    private void OnValidate()
    {
        if (inventory == null)
            inventory = GetComponent<InventorySystem>();
    }

    public void Bind(InventorySystem inv) => inventory = inv;

    public void BindSpawner(AnimalSpawner spawner) => animalSpawner = spawner;

    public ResourceCost[] GetPurchaseCosts(FarmAnimalKind kind)
    {
        return kind switch
        {
            FarmAnimalKind.Cow => cowPurchaseCosts,
            FarmAnimalKind.Chicken => chickenPurchaseCosts,
            FarmAnimalKind.Pig => pigPurchaseCosts,
            _ => Array.Empty<ResourceCost>()
        };
    }

    public ResourceCost[] GetAttackUpgradeCosts() => attackUpgradeCosts;

    public ResourceCost[] GetSpeedUpgradeCosts() => speedUpgradeCosts;

    #region Animals

    public bool BuyCow() => TryBuyAnimal(FarmAnimalKind.Cow, cowPrefab, cowPurchaseCosts, "Vaca");

    public bool BuyChicken() => TryBuyAnimal(FarmAnimalKind.Chicken, chickenPrefab, chickenPurchaseCosts, "Gallina");

    public bool BuyPig() => TryBuyAnimal(FarmAnimalKind.Pig, pigPrefab, pigPurchaseCosts, "Cerdo");

    private bool TryBuyAnimal(FarmAnimalKind kind, GameObject prefab, ResourceCost[] costs, string label)
    {
        if (prefab == null || inventory == null) return false;

        if (animalSpawner == null)
            animalSpawner = FindFirstObjectByType<AnimalSpawner>();

        if (animalSpawner == null)
        {
            Debug.LogError("[Shop] Falta AnimalSpawner en la escena.");
            return false;
        }

        if (!TrySpendCosts(costs))
        {
            Debug.LogWarning($"[Shop] No alcanza para comprar {label}.");
            return false;
        }

        if (animalSpawner.TrySpawnPurchasedAnimal(kind, prefab, inventory, out _))
        {
            Debug.Log($"[Shop] Compraste {label}.");
            return true;
        }

        RefundCosts(costs);
        Debug.LogWarning($"[Shop] No se pudo colocar {label} (corral lleno). Recursos devueltos.");
        return false;
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

        if (!TrySpendCosts(attackUpgradeCosts))
        {
            Debug.LogWarning("[Shop] No alcanza para mejorar ataque.");
            return false;
        }

        pc.IncrementAttackTier();
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

        if (!TrySpendCosts(speedUpgradeCosts))
        {
            Debug.LogWarning("[Shop] No alcanza para mejorar velocidad.");
            return false;
        }

        pl.IncrementSpeedTier();
        Debug.Log("[Shop] Velocidad mejorada.");
        return true;
    }

    #endregion

    private bool TrySpendCosts(ResourceCost[] costs)
    {
        if (inventory == null) return false;
        if (costs == null || costs.Length == 0) return true;

        foreach (var c in costs)
        {
            if (c.amount <= 0) continue;
            if (!inventory.CanAfford(c.type, c.amount))
                return false;
        }

        foreach (var c in costs)
        {
            if (c.amount <= 0) continue;
            if (!inventory.Remove(c.type, c.amount))
                return false;
        }

        return true;
    }

    private void RefundCosts(ResourceCost[] costs)
    {
        if (inventory == null || costs == null) return;
        foreach (var c in costs)
        {
            if (c.amount > 0)
                inventory.Add(c.type, c.amount);
        }
    }
}
