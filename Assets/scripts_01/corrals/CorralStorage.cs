using System;
using UnityEngine;

/// <summary>
/// Almacén local del corral: acumula un solo tipo de recurso (leche/huevo/carne) hasta un máximo.
/// No envía nada al inventario del jugador hasta que <see cref="CollectAllTo"/> sea llamado (clic en corral).
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralStorage : MonoBehaviour
{
    [SerializeField] private CorralZone zone;
    [SerializeField] private int baseMaxCapacity = 20;

    private ResourceType _storedType = ResourceType.Milk;
    private int _current;

    /// <summary>Cantidad almacenada (no en inventario del jugador).</summary>
    public int StoredAmount => _current;

    public ResourceType StoredResourceType => _storedType;

    public int MaxCapacity
    {
        get
        {
            var global = PowerUpSystem.Instance != null ? PowerUpSystem.Instance.BonusCorralStorageCapacity : 0;
            var per = 0;
            if (zone != null && CorralUpgradeSystem.Instance != null)
                per = CorralUpgradeSystem.Instance.GetExtraStorageSlots(zone.AllowedKind);
            return Mathf.Max(1, baseMaxCapacity + global + per);
        }
    }

    public bool IsFull => _current >= MaxCapacity;

    public float FillNormalized => MaxCapacity <= 0 ? 0f : Mathf.Clamp01((float)_current / MaxCapacity);

    public event Action<int, int> StorageChanged;
    public event Action BecameFull;
    public event Action CollectedOrEmptied;

    private void Awake()
    {
        if (zone == null)
            zone = GetComponent<CorralZone>();
        if (zone != null)
            ApplyKind(zone.AllowedKind);
    }

    /// <summary>Enlaza zona y tipo de recurso según especie del corral.</summary>
    public void Initialize(CorralZone corralZone)
    {
        zone = corralZone != null ? corralZone : GetComponent<CorralZone>();
        if (zone != null)
            ApplyKind(zone.AllowedKind);
    }

    private void ApplyKind(FarmAnimalKind kind)
    {
        _storedType = kind switch
        {
            FarmAnimalKind.Cow => ResourceType.Milk,
            FarmAnimalKind.Chicken => ResourceType.Egg,
            FarmAnimalKind.Pig => ResourceType.Meat,
            _ => ResourceType.Meat
        };
    }

    /// <summary>Intenta añadir producción al almacén. Devuelve la cantidad que cabió.</summary>
    public int TryAdd(int amount)
    {
        if (amount <= 0)
            return 0;
        if (!IsCorralOperational())
            return 0;

        var space = Mathf.Max(0, MaxCapacity - _current);
        var add = Mathf.Min(space, amount);
        if (add <= 0)
            return 0;

        var wasFull = _current >= MaxCapacity;
        _current += add;
        StorageChanged?.Invoke(_current, MaxCapacity);

        if (!wasFull && IsFull)
            BecameFull?.Invoke();

        return add;
    }

    /// <summary>Transfiere todo el almacén al inventario del jugador y vacía el corral.</summary>
    public int CollectAllTo(InventorySystem inventory)
    {
        if (inventory == null || _current <= 0)
            return 0;
        if (!IsCorralOperational())
            return 0;

        var t = _current;
        _current = 0;
        inventory.Add(_storedType, t);
        StorageChanged?.Invoke(_current, MaxCapacity);
        CollectedOrEmptied?.Invoke();
        return t;
    }

    public void NotifyMaxCapacityMayHaveChanged()
    {
        StorageChanged?.Invoke(_current, MaxCapacity);
        if (IsFull)
            BecameFull?.Invoke();
    }

    private bool IsCorralOperational()
    {
        var h = zone != null ? zone.GetComponent<CorralHealth>() : GetComponent<CorralHealth>();
        return h == null || !h.IsDestroyed;
    }
}
