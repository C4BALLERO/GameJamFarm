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
    [SerializeField] private PowerUpSystem powerUpSystem;

    [Header("Animal Prefabs")]
    [SerializeField] private GameObject cowPrefab;
    [SerializeField] private GameObject chickenPrefab;
    [SerializeField] private GameObject pigPrefab;

    [Header("Costes animales (solo Leche / Huevos / Carne)")]
    [SerializeField] private ResourceCost[] chickenPurchaseCosts =
    {
        new ResourceCost { type = ResourceType.Egg, amount = 4 },
        new ResourceCost { type = ResourceType.Milk, amount = 2 }
    };

    [SerializeField] private ResourceCost[] pigPurchaseCosts =
    {
        new ResourceCost { type = ResourceType.Meat, amount = 9 },
        new ResourceCost { type = ResourceType.Egg, amount = 6 }
    };

    [SerializeField] private ResourceCost[] cowPurchaseCosts =
    {
        new ResourceCost { type = ResourceType.Milk, amount = 12 },
        new ResourceCost { type = ResourceType.Egg, amount = 10 },
        new ResourceCost { type = ResourceType.Meat, amount = 12 }
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

    [Header("Power-Ups (granero)")]
    [SerializeField] private ResourceCost[] fasterGenerationCosts =
    {
        new ResourceCost { type = ResourceType.Egg, amount = 10 },
        new ResourceCost { type = ResourceType.Milk, amount = 7 }
    };
    [SerializeField] private ResourceCost[] animalHealthCosts =
    {
        new ResourceCost { type = ResourceType.Meat, amount = 10 },
        new ResourceCost { type = ResourceType.Milk, amount = 6 }
    };
    [SerializeField] private ResourceCost[] playerDamageBoostCosts =
    {
        new ResourceCost { type = ResourceType.Meat, amount = 12 },
        new ResourceCost { type = ResourceType.Egg, amount = 10 }
    };
    [SerializeField] private ResourceCost[] playerMoveBoostCosts =
    {
        new ResourceCost { type = ResourceType.Egg, amount = 12 },
        new ResourceCost { type = ResourceType.Milk, amount = 8 }
    };
    [SerializeField] private ResourceCost[] reducedSpawnDelayCosts =
    {
        new ResourceCost { type = ResourceType.Meat, amount = 14 },
        new ResourceCost { type = ResourceType.Egg, amount = 8 }
    };

    [Header("Restaurar vida jugador")]
    [SerializeField] private ResourceCost[] playerHealthRestoreCosts =
    {
        new ResourceCost { type = ResourceType.Meat, amount = 5 },
        new ResourceCost { type = ResourceType.Milk, amount = 3 }
    };
    [SerializeField] private int healthRestoreAmount = 3;

    private void Awake()
    {
        if (animalSpawner == null)
            animalSpawner = FindFirstObjectByType<AnimalSpawner>();
        if (powerUpSystem == null)
            powerUpSystem = FindFirstObjectByType<PowerUpSystem>();
    }

    private void OnValidate()
    {
        if (inventory == null)
            inventory = GetComponent<InventorySystem>();
    }

    public void Bind(InventorySystem inv) => inventory = inv;

    public void BindSpawner(AnimalSpawner spawner) => animalSpawner = spawner;

    public void BindPowerUps(PowerUpSystem system) => powerUpSystem = system;

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

    public ResourceCost[] GetPlayerHealthRestoreCosts() => playerHealthRestoreCosts;
    public ResourceCost[] GetAttackUpgradeCosts() => attackUpgradeCosts;

    public ResourceCost[] GetSpeedUpgradeCosts() => speedUpgradeCosts;
    public ResourceCost[] GetFasterGenerationCosts() => fasterGenerationCosts;
    public ResourceCost[] GetAnimalHealthCosts() => animalHealthCosts;
    public ResourceCost[] GetPlayerDamageBoostCosts() => playerDamageBoostCosts;
    public ResourceCost[] GetPlayerMoveBoostCosts() => playerMoveBoostCosts;
    public ResourceCost[] GetReducedSpawnDelayCosts() => reducedSpawnDelayCosts;

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

    #region PowerUps

    public bool BuyFasterGenerationPowerUp() =>
        TryBuyPowerUp(fasterGenerationCosts, "Generacion rapida", p => p.BuyResourceGenerationBoost());

    public bool BuyAnimalHealthPowerUp() =>
        TryBuyPowerUp(animalHealthCosts, "Vida animal", p => p.BuyAnimalHealthBoost());

    public bool BuyPlayerDamagePowerUp() =>
        TryBuyPowerUp(playerDamageBoostCosts, "Danio jugador", p => p.BuyPlayerDamageBoost());

    public bool BuyPlayerMovePowerUp() =>
        TryBuyPowerUp(playerMoveBoostCosts, "Movimiento jugador", p => p.BuyPlayerMoveBoost());

    public bool BuyReducedSpawnDelayPowerUp() =>
        TryBuyPowerUp(reducedSpawnDelayCosts, "Reducir spawn enemigo", p => p.BuySpawnDelayReductionBoost());

    public bool BuyPlayerHealthRestore()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph == null) { Debug.LogWarning("[Shop] No hay PlayerHealth."); return false; }
        if (!TrySpendCosts(playerHealthRestoreCosts)) { Debug.LogWarning("[Shop] No alcanza para restaurar vida."); return false; }
        ph.Heal(healthRestoreAmount);
        Debug.Log($"[Shop] Vida restaurada: +{healthRestoreAmount}.");
        return true;
    }

    private bool TryBuyPowerUp(ResourceCost[] costs, string label, Action<PowerUpSystem> apply)
    {
        if (powerUpSystem == null)
            powerUpSystem = FindFirstObjectByType<PowerUpSystem>();
        if (powerUpSystem == null || apply == null)
            return false;

        if (!TrySpendCosts(costs))
        {
            Debug.LogWarning($"[Shop] No alcanza para power-up: {label}.");
            return false;
        }

        apply(powerUpSystem);
        Debug.Log($"[Shop] Power-up comprado: {label}.");
        return true;
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
