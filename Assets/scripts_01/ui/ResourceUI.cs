using UnityEngine;
using UnityEngine.UI;

public sealed class ResourceUI : MonoBehaviour
{
    [SerializeField] private Text meatText;
    [SerializeField] private Text bloodText;
    [SerializeField] private Text essenceText;

    public void Bind(InventorySystem inventory)
    {
        if (inventory == null) return;
        inventory.ResourceChanged += OnResourceChanged;
        Refresh(inventory);
    }

    private void OnResourceChanged(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Meat:
                if (meatText != null) meatText.text = amount.ToString();
                break;
            case ResourceType.Blood:
                if (bloodText != null) bloodText.text = amount.ToString();
                break;
            case ResourceType.Essence:
                if (essenceText != null) essenceText.text = amount.ToString();
                break;
        }
    }

    private void Refresh(InventorySystem inventory)
    {
        if (meatText != null) meatText.text = inventory.Get(ResourceType.Meat).ToString();
        if (bloodText != null) bloodText.text = inventory.Get(ResourceType.Blood).ToString();
        if (essenceText != null) essenceText.text = inventory.Get(ResourceType.Essence).ToString();
    }
}

