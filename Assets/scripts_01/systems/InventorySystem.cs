using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InventorySystem : MonoBehaviour
{
    public event Action<ResourceType, int> ResourceChanged;

    private readonly Dictionary<ResourceType, int> _amounts = new();

    public int Get(ResourceType type) => _amounts.TryGetValue(type, out var v) ? v : 0;

    public void Add(ResourceType type, int amount)
    {
        if (amount <= 0) return;
        var newAmount = Get(type) + amount;
        _amounts[type] = newAmount;
        ResourceChanged?.Invoke(type, newAmount);
    }

    public bool Remove(ResourceType type, int amount)
    {
        if (amount <= 0) return true;
        var cur = Get(type);
        if (cur < amount) return false;
        var newAmount = cur - amount;
        _amounts[type] = newAmount;
        ResourceChanged?.Invoke(type, newAmount);
        return true;
    }
}

