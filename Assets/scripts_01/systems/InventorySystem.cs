using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Inventario solo Leche / Huevos / Carne.</summary>
[DisallowMultipleComponent]
public sealed class InventorySystem : MonoBehaviour
{
    [Header("Recursos iniciales")]
    [SerializeField] private int startingMilk = 8;
    [SerializeField] private int startingEgg = 8;
    [SerializeField] private int startingMeat = 6;

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
        _amounts[ResourceType.Milk] = Mathf.Max(0, startingMilk);
        _amounts[ResourceType.Egg] = Mathf.Max(0, startingEgg);
        _amounts[ResourceType.Meat] = Mathf.Max(0, startingMeat);

        foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
            ResourceChanged?.Invoke(t, Get(t));
    }

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

    public bool CanAfford(ResourceType type, int amount) => Get(type) >= amount;

    public void ClearAll()
    {
        foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
        {
            _amounts[t] = 0;
            ResourceChanged?.Invoke(t, 0);
        }
    }
}
