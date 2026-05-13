using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Inventario Leche / Huevos / Carne / Monedas con tope en recursos principales.</summary>
[DisallowMultipleComponent]
public sealed class InventorySystem : MonoBehaviour
{
    [Header("Recursos iniciales")]
    [SerializeField] private int startingMilk = 0;
    [SerializeField] private int startingEgg = 0;
    [SerializeField] private int startingMeat = 0;
    [SerializeField] private int startingCoins = 18;

    [Header("Capacidad máxima (inventario principal)")]
    [SerializeField] private int maxMilkStorage = 100;
    [SerializeField] private int maxEggStorage = 100;
    [SerializeField] private int maxMeatStorage = 100;
    [SerializeField] private int maxFeedBasicStorage = 200;
    [SerializeField] private int maxFeedPremiumStorage = 80;

    public event Action<ResourceType, int> ResourceChanged;

    private readonly Dictionary<ResourceType, int> _amounts = new();

    private void Awake()
    {
        foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
        {
            if (!_amounts.ContainsKey(t))
                _amounts[t] = 0;
        }
    }

    private void Start()
    {
        _amounts[ResourceType.Milk] = Mathf.Clamp(Mathf.Max(0, startingMilk), 0, GetStorageMax(ResourceType.Milk));
        _amounts[ResourceType.Egg] = Mathf.Clamp(Mathf.Max(0, startingEgg), 0, GetStorageMax(ResourceType.Egg));
        _amounts[ResourceType.Meat] = Mathf.Clamp(Mathf.Max(0, startingMeat), 0, GetStorageMax(ResourceType.Meat));
        _amounts[ResourceType.Coin] = Mathf.Max(0, startingCoins);
        _amounts[ResourceType.FeedBasic] = Mathf.Clamp(_amounts[ResourceType.FeedBasic], 0, GetStorageMax(ResourceType.FeedBasic));
        _amounts[ResourceType.FeedPremium] = Mathf.Clamp(_amounts[ResourceType.FeedPremium], 0, GetStorageMax(ResourceType.FeedPremium));

        foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            ResourceChanged?.Invoke(t, Get(t));
    }

    public int Get(ResourceType type) => _amounts.TryGetValue(type, out var v) ? v : 0;

    /// <summary>Tope para leche/huevo/carne. Monedas sin tope práctico.</summary>
    public int GetStorageMax(ResourceType type)
    {
        return type switch
        {
            ResourceType.Milk => Mathf.Max(1, maxMilkStorage),
            ResourceType.Egg => Mathf.Max(1, maxEggStorage),
            ResourceType.Meat => Mathf.Max(1, maxMeatStorage),
            ResourceType.Coin => int.MaxValue / 4,
            ResourceType.FeedBasic => Mathf.Max(1, maxFeedBasicStorage),
            ResourceType.FeedPremium => Mathf.Max(1, maxFeedPremiumStorage),
            _ => int.MaxValue / 4
        };
    }

    public int GetFreeSpace(ResourceType type) => Mathf.Max(0, GetStorageMax(type) - Get(type));

    /// <summary>Añade cantidad respetando tope. Devuelve unidades que realmente entraron.</summary>
    public int Add(ResourceType type, int amount)
    {
        if (amount <= 0)
            return 0;

        var max = GetStorageMax(type);
        var cur = Get(type);
        if (type == ResourceType.Coin)
        {
            var next = cur + amount;
            if (next < cur)
                next = int.MaxValue;
            _amounts[type] = next;
            ResourceChanged?.Invoke(type, next);
            return amount;
        }

        var space = Mathf.Max(0, max - cur);
        var add = Mathf.Min(space, amount);
        if (add <= 0)
            return 0;

        var newAmount = cur + add;
        _amounts[type] = newAmount;
        ResourceChanged?.Invoke(type, newAmount);
        return add;
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
        foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
        {
            _amounts[t] = 0;
            ResourceChanged?.Invoke(t, 0);
        }
    }
}
