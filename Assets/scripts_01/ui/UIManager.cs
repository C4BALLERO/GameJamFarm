using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
        if (WasTabPressed())
        {
            if (shopPanelRoot != null)
                shopPanelRoot.SetActive(!shopPanelRoot.activeSelf);
        }
    }

    private static bool WasTabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
    }

    #region Shop Actions

    public void BuyCow() => shopSystem?.BuyCow();
    public void BuyChicken() => shopSystem?.BuyChicken();
    public void BuyPig() => shopSystem?.BuyPig();
    
    public void BuyHoe() => shopSystem?.BuyHoe();
    public void BuyAxe() => shopSystem?.BuyAxe();
    public void BuyPickaxe() => shopSystem?.BuyPickaxe();

    public void SellMilkBatch() => shopSystem?.SellMilkBatch();
    public void SellEggBatch() => shopSystem?.SellEggBatch();
    public void SellMeatBatch() => shopSystem?.SellMeatBatch();

    public void SellCow() => shopSystem?.SellCow();
    public void SellChicken() => shopSystem?.SellChicken();
    public void SellPig() => shopSystem?.SellPig();

    #endregion
}

