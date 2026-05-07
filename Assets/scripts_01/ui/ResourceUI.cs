using UnityEngine;
using UnityEngine.UI;

/// <summary>Muestra Leche, Huevos y Carne en el HUD con iconos opcionales.</summary>
public sealed class ResourceUI : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private Text milkText;
    [SerializeField] private Text eggText;
    [SerializeField] private Text meatText;

    [Header("Iconos (opcional)")]
    [SerializeField] private Image milkIcon;
    [SerializeField] private Image eggIcon;
    [SerializeField] private Image meatIcon;

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
            case ResourceType.Milk: SetMilk(amount); break;
            case ResourceType.Egg:  SetEgg(amount);  break;
            case ResourceType.Meat: SetMeat(amount); break;
        }
    }

    // Si hay icono asignado, muestra solo el número; si no, muestra "Leche X"
    private void SetMilk(int v) { if (milkText != null) milkText.text = milkIcon != null ? $"{v}" : $"Leche {v}"; }
    private void SetEgg(int v)  { if (eggText  != null) eggText.text  = eggIcon  != null ? $"{v}" : $"Huevos {v}"; }
    private void SetMeat(int v) { if (meatText != null) meatText.text = meatIcon != null ? $"{v}" : $"Carne {v}"; }

    private void Refresh(InventorySystem inventory)
    {
        SetMilk(inventory.Get(ResourceType.Milk));
        SetEgg(inventory.Get(ResourceType.Egg));
        SetMeat(inventory.Get(ResourceType.Meat));
    }
}
