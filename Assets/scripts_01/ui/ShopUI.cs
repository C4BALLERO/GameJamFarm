using UnityEngine;
using UnityEngine.UI;

/// <summary>Panel tienda Granero: precios en monedas, venta de recursos, pausa al abrirse.</summary>
[DisallowMultipleComponent]
public sealed class ShopUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject panelRoot;

    [Header("Refs")]
    [SerializeField] private ShopSystem shopSystem;
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private FarmShopCatalog shopCatalog;

    [Header("Animales: nombres y recurso (opcional; si no hay Text, se ignora)")]
    [SerializeField] private Text cowNameText;
    [SerializeField] private Text cowResourceLineText;
    [SerializeField] private Text chickenNameText;
    [SerializeField] private Text chickenResourceLineText;
    [SerializeField] private Text pigNameText;
    [SerializeField] private Text pigResourceLineText;

    [Header("Power-ups: descripciones (opcional; índice 0..5 = prod, vida, daño, mov, spawn, almacén)")]
    [SerializeField] private Text[] powerUpDescriptionTexts = new Text[6];

    [Header("Etiquetas de coste (monedas)")]
    [SerializeField] private Text cowCostText;
    [SerializeField] private Text chickenCostText;
    [SerializeField] private Text pigCostText;
    [SerializeField] private Text attackCostText;
    [SerializeField] private Text speedCostText;
    [SerializeField] private Text fasterGenerationCostText;
    [SerializeField] private Text animalHealthCostText;
    [SerializeField] private Text playerDamagePowerCostText;
    [SerializeField] private Text playerMovePowerCostText;
    [SerializeField] private Text reducedSpawnDelayCostText;
    [SerializeField] private Text corralStoragePowerCostText;
    [SerializeField] private Text playerHealthRestoreCostText;
    [SerializeField] private Text currentResourcesText;

    [Header("Opcional: línea solo monedas en HUD de tienda")]
    [SerializeField] private Text coinsOnlyText;
    [SerializeField] private Image shopCoinIcon;

    [Header("Venta: vista previa exacta (cantidad → monedas)")]
    [SerializeField] private Text milkSellPreviewText;
    [SerializeField] private Text eggSellPreviewText;
    [SerializeField] private Text meatSellPreviewText;

    [Header("UI generada si faltan botones de venta en la escena")]
    [SerializeField] private bool ensureRuntimeSellButtons = true;

    private bool _didEnsureRuntimeUi;

    private void Awake()
    {
        if (shopSystem == null)
            shopSystem = FindFirstObjectByType<ShopSystem>();
        if (inventory == null)
            inventory = FindFirstObjectByType<InventorySystem>();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>Opción para escenas montadas a mano (sin ShopUIService del builder).</summary>
    public void BindScene(GameObject root, ShopSystem shop, InventorySystem inv)
    {
        panelRoot = root;
        shopSystem = shop;
        inventory = inv;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.ResourceChanged += OnInventoryChanged;

        RefreshLabels();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.ResourceChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged(ResourceType arg1, int arg2) => RefreshLabels();

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        EnsureRuntimeSellRowOnce();
        RefreshLabels();

        if (GameManager.Instance != null)
            GameManager.Instance.SetShopPaused(true);
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetShopPaused(false);
    }

    public void Toggle()
    {
        if (panelRoot == null)
            return;

        var next = !panelRoot.activeSelf;
        panelRoot.SetActive(next);

        if (GameManager.Instance != null)
            GameManager.Instance.SetShopPaused(next);

        if (next)
        {
            EnsureRuntimeSellRowOnce();
            RefreshLabels();
        }
    }

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void RefreshLabels()
    {
        if (shopSystem == null)
            shopSystem = FindFirstObjectByType<ShopSystem>();

        if (shopSystem == null)
            return;

        FormatCoin(cowCostText, shopSystem.GetAnimalCoinPrice(FarmAnimalKind.Cow));
        FormatCoin(chickenCostText, shopSystem.GetAnimalCoinPrice(FarmAnimalKind.Chicken));
        FormatCoin(pigCostText, shopSystem.GetAnimalCoinPrice(FarmAnimalKind.Pig));
        FormatCoin(attackCostText, shopSystem.GetAttackUpgradeCoinCost());
        FormatCoin(speedCostText, shopSystem.GetSpeedUpgradeCoinCost());
        FormatCoin(fasterGenerationCostText, shopSystem.GetPowerUpCoinCost(0));
        FormatCoin(animalHealthCostText, shopSystem.GetPowerUpCoinCost(1));
        FormatCoin(playerDamagePowerCostText, shopSystem.GetPowerUpCoinCost(2));
        FormatCoin(playerMovePowerCostText, shopSystem.GetPowerUpCoinCost(3));
        FormatCoin(reducedSpawnDelayCostText, shopSystem.GetPowerUpCoinCost(4));
        FormatCoin(corralStoragePowerCostText, shopSystem.GetPowerUpCoinCost(5));
        FormatCoin(playerHealthRestoreCostText, shopSystem.GetPlayerHealCoinCost());
        RefreshCurrentResources();
        RefreshCatalogTexts();
        RefreshSellPreviews();
        ApplyShopCoinIcon();
    }

    private static void FormatCoin(Text label, int coins)
    {
        if (label == null) return;
        label.text = coins <= 0 ? "—" : $"{coins} monedas";
    }

    private void RefreshCurrentResources()
    {
        if (inventory == null)
            return;

        var milk = inventory.Get(ResourceType.Milk);
        var egg = inventory.Get(ResourceType.Egg);
        var meat = inventory.Get(ResourceType.Meat);
        var coins = inventory.Get(ResourceType.Coin);

        if (currentResourcesText != null)
        {
            currentResourcesText.text =
                $"Monedas: {coins}   Leche: {milk}   Huevos: {egg}   Carne: {meat}";
        }

        if (coinsOnlyText != null)
            coinsOnlyText.text = $"Monedas: {coins}";
    }

    private void RefreshSellPreviews()
    {
        if (inventory == null)
            return;

        var econ = EconomySystem.Instance != null
            ? EconomySystem.Instance
            : Object.FindFirstObjectByType<EconomySystem>();
        if (econ == null)
            return;

        var mult = BarnUpgradeSystem.Instance != null ? BarnUpgradeSystem.Instance.SellRewardMultiplier : 1f;

        void Line(Text label, ResourceType type, string resourceName)
        {
            if (label == null)
                return;
            var n = inventory.Get(type);
            var per = econ.GetSellCoinsPerUnit(type);
            var gain = Mathf.RoundToInt(n * per * mult);
            label.text = $"{n} {resourceName} → +{gain} monedas";
        }

        Line(milkSellPreviewText, ResourceType.Milk, "leche");
        Line(eggSellPreviewText, ResourceType.Egg, "huevos");
        Line(meatSellPreviewText, ResourceType.Meat, "carne");
    }

    private void ApplyShopCoinIcon()
    {
        if (shopCoinIcon == null)
            return;
        var econ = EconomySystem.Instance != null
            ? EconomySystem.Instance
            : Object.FindFirstObjectByType<EconomySystem>();
        if (econ == null)
            return;
        var s = econ.CoinSprite;
        if (s != null)
            shopCoinIcon.sprite = s;
    }

    private void EnsureRuntimeSellRowOnce()
    {
        if (!ensureRuntimeSellButtons || _didEnsureRuntimeUi || panelRoot == null)
            return;

        var root = panelRoot.transform;
        if (root.Find("RuntimeSellRow") != null)
        {
            _didEnsureRuntimeUi = true;
            return;
        }

        var row = new GameObject("RuntimeSellRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.layer = root.gameObject.layer;
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.SetParent(root, false);
        rowRt.anchorMin = new Vector2(0.04f, 0.125f);
        rowRt.anchorMax = new Vector2(0.96f, 0.195f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.offsetMin = Vector2.zero;
        rowRt.offsetMax = Vector2.zero;

        var h = row.GetComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.spacing = 8f;
        h.childForceExpandHeight = true;
        h.childForceExpandWidth = true;
        h.padding = new RectOffset(4, 4, 2, 2);

        CreateSellButton(row.transform, "BtnVenderLeche", "Vender leche", OnSellMilk);
        CreateSellButton(row.transform, "BtnVenderHuevos", "Vender huevos", OnSellEgg);
        CreateSellButton(row.transform, "BtnVenderCarne", "Vender carne", OnSellMeat);
        CreateSellButton(row.transform, "BtnAlmacenCorral", "Más almacén", OnBuyCorralStoragePowerUp);

        _didEnsureRuntimeUi = true;
    }

    private static void CreateSellButton(Transform parent, string name, string caption, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        var le = go.GetComponent<LayoutElement>();
        le.minWidth = 120f;
        le.preferredHeight = 32f;
        le.flexibleWidth = 1f;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.22f, 0.38f, 0.24f, 0.95f);
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.layer = parent.gameObject.layer;
        var trt = textGo.GetComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        var tx = textGo.GetComponent<Text>();
        tx.font = font;
        tx.fontSize = 13;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = Color.white;
        tx.text = caption;
        tx.raycastTarget = false;
    }

    private static SellSystem ResolveSell()
    {
        return SellSystem.Instance != null
            ? SellSystem.Instance
            : Object.FindFirstObjectByType<SellSystem>();
    }

    public void OnSellMilk()
    {
        if (ResolveSell()?.SellAll(ResourceType.Milk) == true)
            RefreshLabels();
    }

    public void OnSellEgg()
    {
        if (ResolveSell()?.SellAll(ResourceType.Egg) == true)
            RefreshLabels();
    }

    public void OnSellMeat()
    {
        if (ResolveSell()?.SellAll(ResourceType.Meat) == true)
            RefreshLabels();
    }

    // Wrappers públicos para onClick de botones en el editor
    public void OnBuyCow() => shopSystem?.BuyCow();
    public void OnBuyChicken() => shopSystem?.BuyChicken();
    public void OnBuyPig() => shopSystem?.BuyPig();
    public void OnBuyAttackUpgrade() => shopSystem?.BuyAttackUpgrade();
    public void OnBuySpeedUpgrade() => shopSystem?.BuySpeedUpgrade();
    public void OnBuyPlayerHealthRestore() => shopSystem?.BuyPlayerHealthRestore();
    public void OnBuyFasterGenerationPowerUp() => shopSystem?.BuyFasterGenerationPowerUp();
    public void OnBuyAnimalHealthPowerUp() => shopSystem?.BuyAnimalHealthPowerUp();
    public void OnBuyPlayerDamagePowerUp() => shopSystem?.BuyPlayerDamagePowerUp();
    public void OnBuyPlayerMovePowerUp() => shopSystem?.BuyPlayerMovePowerUp();
    public void OnBuyReducedSpawnDelayPowerUp() => shopSystem?.BuyReducedSpawnDelayPowerUp();
    public void OnBuyCorralStoragePowerUp() => shopSystem?.BuyCorralStoragePowerUp();

    private void RefreshCatalogTexts()
    {
        var c = shopCatalog;
        if (cowNameText != null)
            cowNameText.text = c != null ? c.cowName : "Vaca";
        if (cowResourceLineText != null)
            cowResourceLineText.text = c != null ? c.cowResourceLine : "Produce: leche (corral)";
        if (chickenNameText != null)
            chickenNameText.text = c != null ? c.chickenName : "Gallina";
        if (chickenResourceLineText != null)
            chickenResourceLineText.text = c != null ? c.chickenResourceLine : "Produce: huevos (corral)";
        if (pigNameText != null)
            pigNameText.text = c != null ? c.pigName : "Cerdo";
        if (pigResourceLineText != null)
            pigResourceLineText.text = c != null ? c.pigResourceLine : "Produce: carne (corral)";

        if (powerUpDescriptionTexts == null)
            return;

        for (var i = 0; i < powerUpDescriptionTexts.Length && i < 6; i++)
        {
            var t = powerUpDescriptionTexts[i];
            if (t == null) continue;
            t.text = c != null ? c.GetPowerUpDescription(i) : DefaultPowerUpDescription(i);
        }
    }

    private static string DefaultPowerUpDescription(int index)
    {
        return index switch
        {
            0 => "Acelera la producción en los corrales.",
            1 => "Más vida para los animales.",
            2 => "Más daño del jugador.",
            3 => "Más velocidad del jugador.",
            4 => "Más tiempo entre spawns de enemigos.",
            5 => "Más capacidad máxima en cada corral.",
            _ => string.Empty
        };
    }
}
