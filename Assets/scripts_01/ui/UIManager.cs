using UnityEngine;

/// <summary>
/// Main UI manager that coordinates all UI elements and handles UI interactions.
/// </summary>
[DisallowMultipleComponent]
public sealed class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private ResourceUI resourceUI;
    [SerializeField] private GameObject shopPanelRoot;

    [Header("Shop")]
    [SerializeField] private ShopSystem shopSystem;

    /// <summary>
    /// Bind UI elements to player systems
    /// </summary>
    public void Bind(PlayerHealth playerHealth, InventorySystem inventory)
    {
        if (healthBar != null) 
            healthBar.Bind(playerHealth);
        if (resourceUI != null) 
            resourceUI.Bind(inventory);
    }

    private void Update()
    {
        // Toggle shop panel with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (shopPanelRoot != null)
                shopPanelRoot.SetActive(!shopPanelRoot.activeSelf);
        }
    }

    #region Shop Actions

    public void BuyCow() => shopSystem?.BuyCow();
    public void BuyChicken() => shopSystem?.BuyChicken();
    public void BuyPig() => shopSystem?.BuyPig();
    
    public void BuyHoe() => shopSystem?.BuyHoe();
    public void BuyAxe() => shopSystem?.BuyAxe();
    public void BuyPickaxe() => shopSystem?.BuyPickaxe();

    #endregion
}

