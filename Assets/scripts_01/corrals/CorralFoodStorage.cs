using System;
using UnityEngine;

/// <summary>
/// Comida almacenada en un corral. Los animales la consumen vía <see cref="CorralFoodTicker"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralFoodStorage : MonoBehaviour
{
    [SerializeField] private CorralZone zone;
    [SerializeField] private int baseMaxFood = 48;

    public event Action<int, int> FoodChanged;

    public int CurrentFood { get; private set; }
    public int MaxFood { get; private set; }

    private bool _listeningUpgrades;

    private void Awake()
    {
        if (zone == null)
            zone = GetComponent<CorralZone>();
        RecalculateMax();
        CurrentFood = Mathf.Clamp(CurrentFood, 0, MaxFood);
    }

    private void OnDestroy() => UnregisterUpgradeWatch();

    public void Initialize(CorralZone z)
    {
        zone = z != null ? z : GetComponent<CorralZone>();
        RecalculateMax();
        FoodChanged?.Invoke(CurrentFood, MaxFood);
    }

    /// <summary>Llamar desde <see cref="CorralManager"/> cuando <see cref="CorralUpgradeSystem"/> ya existe.</summary>
    public void RegisterUpgradeWatch()
    {
        if (_listeningUpgrades)
            return;
        if (CorralUpgradeSystem.Instance != null)
        {
            CorralUpgradeSystem.Instance.UpgradesChanged += OnUpgradesChanged;
            _listeningUpgrades = true;
        }
    }

    public void UnregisterUpgradeWatch()
    {
        if (!_listeningUpgrades)
            return;
        if (CorralUpgradeSystem.Instance != null)
            CorralUpgradeSystem.Instance.UpgradesChanged -= OnUpgradesChanged;
        _listeningUpgrades = false;
    }

    private void OnUpgradesChanged() => RecalculateMax();

    /// <summary>Capacidad escala un poco con mejoras de almacén de recursos del mismo corral.</summary>
    public void RecalculateMax()
    {
        if (zone == null)
            zone = GetComponent<CorralZone>();

        var bonusSlots = 0;
        if (zone != null && CorralUpgradeSystem.Instance != null)
            bonusSlots = CorralUpgradeSystem.Instance.GetExtraStorageSlots(zone.AllowedKind);

        MaxFood = Mathf.Max(12, baseMaxFood + bonusSlots * 2);
        CurrentFood = Mathf.Clamp(CurrentFood, 0, MaxFood);
        FoodChanged?.Invoke(CurrentFood, MaxFood);
    }

    /// <summary>
    /// Añade comida sin superar nunca <see cref="MaxFood"/>. Devuelve cuántas unidades se añadieron realmente (0 si lleno).
    /// </summary>
    public int TryAddFood(int amount)
    {
        if (amount <= 0 || MaxFood <= 0)
            return 0;
        CurrentFood = Mathf.Clamp(CurrentFood, 0, MaxFood);
        var space = Mathf.Max(0, MaxFood - CurrentFood);
        var add = Mathf.Min(space, amount);
        if (add <= 0)
            return 0;
        CurrentFood += add;
        CurrentFood = Mathf.Clamp(CurrentFood, 0, MaxFood);
        FoodChanged?.Invoke(CurrentFood, MaxFood);
        return add;
    }

    /// <summary>Retira hasta <paramref name="amount"/> unidades; devuelve cuánto se retiró.</summary>
    public int ConsumeUpTo(int amount)
    {
        if (amount <= 0 || CurrentFood <= 0)
            return 0;
        var take = Mathf.Min(CurrentFood, amount);
        CurrentFood -= take;
        FoodChanged?.Invoke(CurrentFood, MaxFood);
        return take;
    }

    public float NormalizedFood => MaxFood <= 0 ? 0f : (float)CurrentFood / MaxFood;
}
