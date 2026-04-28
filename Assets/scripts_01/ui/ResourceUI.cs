using UnityEngine;
using UnityEngine.UI;

/// <summary>Solo Leche, Huevos y Carne en pantalla.</summary>
public sealed class ResourceUI : MonoBehaviour
{
    [SerializeField] private Text milkText;
    [SerializeField] private Text eggText;
    [SerializeField] private Text meatText;

    private InventorySystem _inventory;

    public void Bind(InventorySystem inventory)
    {
        if (inventory == null) return;
        if (_inventory != null)
            _inventory.ResourceChanged -= OnResourceChanged;

        _inventory = inventory;
        _inventory.ResourceChanged += OnResourceChanged;
        Refresh(_inventory);
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
            case ResourceType.Milk:
                SetMilk(amount);
                break;
            case ResourceType.Egg:
                SetEgg(amount);
                break;
            case ResourceType.Meat:
                SetMeat(amount);
                break;
        }
    }

    private void SetMilk(int v)
    {
        if (milkText != null) milkText.text = $"Leche {v}";
    }

    private void SetEgg(int v)
    {
        if (eggText != null) eggText.text = $"Huevos {v}";
    }

    private void SetMeat(int v)
    {
        if (meatText != null) meatText.text = $"Carne {v}";
    }

    private void Refresh(InventorySystem inventory)
    {
        SetMilk(inventory.Get(ResourceType.Milk));
        SetEgg(inventory.Get(ResourceType.Egg));
        SetMeat(inventory.Get(ResourceType.Meat));
    }
}
