using UnityEngine;

public sealed class UIManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private ResourceUI resourceUI;
    [SerializeField] private GameObject shopPanelRoot;

    [Header("Shop")]
    [SerializeField] private ShopSystem shopSystem;

    public void Bind(PlayerHealth playerHealth, InventorySystem inventory)
    {
        if (healthBar != null) healthBar.Bind(playerHealth);
        if (resourceUI != null) resourceUI.Bind(inventory);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (shopPanelRoot != null)
                shopPanelRoot.SetActive(!shopPanelRoot.activeSelf);
        }
    }

    public void BuyCow() => shopSystem?.BuyCow();
    public void BuyChicken() => shopSystem?.BuyChicken();
    public void BuyPig() => shopSystem?.BuyPig();
    public void SellMeat() => shopSystem?.SellMeatForEssence();
    public void SellBlood() => shopSystem?.SellBloodForEssence();
}

