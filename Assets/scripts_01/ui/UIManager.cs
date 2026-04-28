using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Coordina HUD y acciones del Granero.</summary>
[DisallowMultipleComponent]
public sealed class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private ResourceUI resourceUI;
    [SerializeField] private GameObject shopPanelRoot;

    [Header("Shop")]
    [SerializeField] private ShopSystem shopSystem;
    [SerializeField] private ShopUI shopUi;

    public void Bind(PlayerHealth playerHealth, InventorySystem inventory)
    {
        if (healthBar != null)
            healthBar.Bind(playerHealth);
        if (resourceUI != null)
            resourceUI.Bind(inventory);
    }

    /// <summary>Llamado desde <see cref="FarmSceneGameplayBootstrap"/> cuando ShopUI se crea en tiempo de ejecución.</summary>
    public void RegisterShopUi(ShopUI ui)
    {
        shopUi = ui;
    }

    private void Update()
    {
        if (WasTabPressed())
        {
            if (shopUi != null)
                shopUi.Toggle();
            else if (shopPanelRoot != null)
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

    #region Granero

    public void BuyCow() => shopSystem?.BuyCow();

    public void BuyChicken() => shopSystem?.BuyChicken();

    public void BuyPig() => shopSystem?.BuyPig();

    public void BuyAttackUpgrade() => shopSystem?.BuyAttackUpgrade();

    public void BuySpeedUpgrade() => shopSystem?.BuySpeedUpgrade();

    public void CloseShop() => shopUi?.Close();

    #endregion
}
