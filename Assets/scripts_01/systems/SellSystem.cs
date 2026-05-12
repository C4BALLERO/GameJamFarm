using UnityEngine;

/// <summary>
/// Convierte leche/huevos/carne del inventario en monedas según <see cref="EconomySystem"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class SellSystem : MonoBehaviour
{
    public static SellSystem Instance { get; private set; }

    [SerializeField] private InventorySystem inventory;
    [SerializeField] private EconomySystem economy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<InventorySystem>();
        if (economy == null)
            economy = EconomySystem.Instance ?? FindFirstObjectByType<EconomySystem>();
    }

    /// <summary>Vende todo el stock de un recurso (no monedas).</summary>
    public bool SellAll(ResourceType type)
    {
        if (type == ResourceType.Coin)
            return false;
        if (inventory == null)
            inventory = FindFirstObjectByType<InventorySystem>();
        if (economy == null)
            economy = EconomySystem.Instance ?? FindFirstObjectByType<EconomySystem>();
        if (inventory == null || economy == null)
            return false;

        var amount = inventory.Get(type);
        if (amount <= 0)
            return false;

        var per = economy.GetSellCoinsPerUnit(type);
        if (per <= 0)
            return false;

        var coins = Mathf.RoundToInt(amount * per * (BarnUpgradeSystem.Instance != null ? BarnUpgradeSystem.Instance.SellRewardMultiplier : 1f));
        if (!inventory.Remove(type, amount))
            return false;

        inventory.Add(ResourceType.Coin, coins);
        return true;
    }
}
