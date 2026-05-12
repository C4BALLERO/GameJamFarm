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
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return;
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

    public void BuyFasterGenerationPowerUp() => shopSystem?.BuyFasterGenerationPowerUp();

    public void BuyAnimalHealthPowerUp() => shopSystem?.BuyAnimalHealthPowerUp();

    public void BuyPlayerDamagePowerUp() => shopSystem?.BuyPlayerDamagePowerUp();

    public void BuyPlayerMovePowerUp() => shopSystem?.BuyPlayerMovePowerUp();

    public void BuyReducedSpawnDelayPowerUp() => shopSystem?.BuyReducedSpawnDelayPowerUp();

    public void BuyCorralStoragePowerUp() => shopSystem?.BuyCorralStoragePowerUp();

    public void SellAllMilk() => TrySell(ResourceType.Milk);

    public void SellAllEggs() => TrySell(ResourceType.Egg);

    public void SellAllMeat() => TrySell(ResourceType.Meat);

    private static void TrySell(ResourceType type)
    {
        var sell = SellSystem.Instance != null
            ? SellSystem.Instance
            : Object.FindFirstObjectByType<SellSystem>();
        sell?.SellAll(type);
    }

    public void CloseShop() => shopUi?.Close();

    public void BuyBarnTierUpgrade() => shopSystem?.BuyBarnTierUpgrade();

    public void BuyCorralStorageUpgradeCow() => shopSystem?.BuyCorralStorageUpgrade(FarmAnimalKind.Cow);

    public void BuyCorralStorageUpgradeChicken() => shopSystem?.BuyCorralStorageUpgrade(FarmAnimalKind.Chicken);

    public void BuyCorralStorageUpgradePig() => shopSystem?.BuyCorralStorageUpgrade(FarmAnimalKind.Pig);

    #endregion
}
