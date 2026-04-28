using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages player inventory and resources.
/// Tracks amounts of each resource type and notifies listeners of changes.
/// </summary>
[DisallowMultipleComponent]
public sealed class InventorySystem : MonoBehaviour
{
    [Header("Starting Resources")]
    [SerializeField] private int startingWood = 100;
    [SerializeField] private int startingStone = 50;
    [SerializeField] private int startingMilk = 0;
    [SerializeField] private int startingEgg = 0;
    [SerializeField] private int startingMeat = 0;
    [SerializeField] private int startingGold = 120;

    public event Action<ResourceType, int> ResourceChanged;

    private readonly Dictionary<ResourceType, int> _amounts = new();

    private void Awake()
    {
        foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (!_amounts.ContainsKey(t))
                _amounts[t] = 0;
        }
    }

    private void Start()
    {
        _amounts[ResourceType.Wood] = startingWood;
        _amounts[ResourceType.Stone] = startingStone;
        _amounts[ResourceType.Milk] = startingMilk;
        _amounts[ResourceType.Egg] = startingEgg;
        _amounts[ResourceType.Meat] = startingMeat;
        _amounts[ResourceType.Gold] = Mathf.Max(0, startingGold);

        foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
            ResourceChanged?.Invoke(t, Get(t));

        Debug.Log("[InventorySystem] Initialized with starting resources");
    }

    /// <summary>
    /// Get amount of specific resource type
    /// </summary>
    public int Get(ResourceType type) => _amounts.TryGetValue(type, out var v) ? v : 0;

    /// <summary>
    /// Add resources to inventory
    /// </summary>
    public void Add(ResourceType type, int amount)
    {
        if (amount <= 0) return;
        var newAmount = Get(type) + amount;
        _amounts[type] = newAmount;
        ResourceChanged?.Invoke(type, newAmount);
        Debug.Log($"[Inventory] Added {amount} x {type}. Total: {newAmount}");
    }

    /// <summary>
    /// Remove resources from inventory
    /// </summary>
    /// <returns>True if successful, false if insufficient resources</returns>
    public bool Remove(ResourceType type, int amount)
    {
        if (amount <= 0) return true;
        var cur = Get(type);
        if (cur < amount) return false;
        var newAmount = cur - amount;
        _amounts[type] = newAmount;
        ResourceChanged?.Invoke(type, newAmount);
        Debug.Log($"[Inventory] Removed {amount} x {type}. Remaining: {newAmount}");
        return true;
    }

    /// <summary>
    /// Check if inventory has enough of a resource
    /// </summary>
    public bool CanAfford(ResourceType type, int amount) => Get(type) >= amount;

    /// <summary>
    /// Get total resources of all types
    /// </summary>
    public int GetTotalResources()
    {
        int total = 0;
        foreach (var amount in _amounts.Values)
            total += amount;
        return total;
    }
}

