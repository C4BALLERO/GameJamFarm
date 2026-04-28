using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays key survival resources tracked by <see cref="InventorySystem"/> (food, gold, crops).
/// </summary>
public sealed class ResourceUI : MonoBehaviour
{
    [SerializeField] private Text milkText;
    [SerializeField] private Text eggText;
    [SerializeField] private Text meatText;
    [SerializeField] private Text goldText;

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
                if (milkText != null) milkText.text = amount.ToString();
                break;
            case ResourceType.Egg:
                if (eggText != null) eggText.text = amount.ToString();
                break;
            case ResourceType.Meat:
                if (meatText != null) meatText.text = amount.ToString();
                break;
            case ResourceType.Gold:
                if (goldText != null) goldText.text = amount.ToString();
                break;
        }
    }

    private void Refresh(InventorySystem inventory)
    {
        if (milkText != null) milkText.text = inventory.Get(ResourceType.Milk).ToString();
        if (eggText != null) eggText.text = inventory.Get(ResourceType.Egg).ToString();
        if (meatText != null) meatText.text = inventory.Get(ResourceType.Meat).ToString();
        if (goldText != null) goldText.text = inventory.Get(ResourceType.Gold).ToString();
    }
}


