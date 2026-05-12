using UnityEngine;
using UnityEngine.UI;

/// <summary>Muestra Leche, Huevos, Carne y Monedas en el HUD con iconos opcionales.</summary>
public sealed class ResourceUI : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private Text milkText;
    [SerializeField] private Text eggText;
    [SerializeField] private Text meatText;
    [SerializeField] private Text coinText;

    [Header("Iconos (opcional)")]
    [SerializeField] private Image milkIcon;
    [SerializeField] private Image eggIcon;
    [SerializeField] private Image meatIcon;
    [SerializeField] private Image coinIcon;

    private InventorySystem _inventory;

    public void Bind(InventorySystem inventory)
    {
        if (inventory == null) return;
        if (_inventory != null)
            _inventory.ResourceChanged -= OnResourceChanged;

        _inventory = inventory;
        _inventory.ResourceChanged += OnResourceChanged;
        ApplyCoinSprite();
        Refresh(_inventory);
    }

    private void ApplyCoinSprite()
    {
        if (coinIcon == null)
            return;
        var econ = EconomySystem.Instance != null
            ? EconomySystem.Instance
            : Object.FindFirstObjectByType<EconomySystem>();
        if (econ == null)
            return;
        var s = econ.CoinSprite;
        if (s != null)
            coinIcon.sprite = s;
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.ResourceChanged -= OnResourceChanged;
        _inventory = null;
    }

    private void OnResourceChanged(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Milk: SetMilk(amount); break;
            case ResourceType.Egg: SetEgg(amount); break;
            case ResourceType.Meat: SetMeat(amount); break;
            case ResourceType.Coin: SetCoin(amount); break;
        }
    }

    private void SetMilk(int v) { if (milkText != null) milkText.text = milkIcon != null ? $"{v}" : $"Leche {v}"; }
    private void SetEgg(int v) { if (eggText != null) eggText.text = eggIcon != null ? $"{v}" : $"Huevos {v}"; }
    private void SetMeat(int v) { if (meatText != null) meatText.text = meatIcon != null ? $"{v}" : $"Carne {v}"; }
    private void SetCoin(int v) { if (coinText != null) coinText.text = coinIcon != null ? $"{v}" : $"Monedas {v}"; }

    private void Refresh(InventorySystem inventory)
    {
        SetMilk(inventory.Get(ResourceType.Milk));
        SetEgg(inventory.Get(ResourceType.Egg));
        SetMeat(inventory.Get(ResourceType.Meat));
        SetCoin(inventory.Get(ResourceType.Coin));
    }
}
