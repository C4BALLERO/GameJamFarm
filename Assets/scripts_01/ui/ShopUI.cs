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
    [SerializeField] private Text sellRulesSummaryText;

    [Header("Tienda: mejoras de corral (opcional)")]
    [SerializeField] private Text corralUpgradesSummaryText;

    [Header("UI generada si faltan botones de venta en la escena")]
    [SerializeField] private bool ensureRuntimeSellButtons = true;

    private bool _didEnsureRuntimeUi;
    private bool _didFoodRow;
    private bool _hidAnimalPurchases;

    private BarnUIManager _newBarnUi;

    private void Awake()
    {
        if (shopSystem == null)
            shopSystem = FindFirstObjectByType<ShopSystem>();
        if (inventory == null)
            inventory = FindFirstObjectByType<InventorySystem>();

        // Eliminar la tienda antigua por completo
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Crear e inicializar el nuevo BarnUIManager (Nueva UI)
        _newBarnUi = gameObject.AddComponent<BarnUIManager>();
        _newBarnUi.Initialize(shopSystem, inventory, transform);

        gameObject.SetActive(false);
    }

    private void HideBarnAnimalPurchaseUi()
    {
        if (_hidAnimalPurchases || panelRoot == null)
            return;
        _hidAnimalPurchases = true;
        var hideNames = new[]
        {
            "BuyCow", "BuyChicken", "BuyPig",
            "CowCostText", "ChickenCostText", "PigCostText",
            "Icon_Cow", "Icon_Chicken", "Icon_Pig"
        };
        foreach (var tr in panelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (tr == null)
                continue;
            foreach (var n in hideNames)
            {
                if (tr.name != n)
                    continue;
                tr.gameObject.SetActive(false);
                break;
            }
        }
    }

    private void StyleShopPanelRoot()
    {
        if (panelRoot == null)
            return;
        var img = panelRoot.GetComponent<Image>();
        if (img != null)
            UIStyleSheet.ApplyPanelImage(img, 0.93f);
    }

    /// <summary>Opción para escenas montadas a mano (sin ShopUIService del builder).</summary>
    public void BindScene(GameObject root, ShopSystem shop, InventorySystem inv)
    {
        panelRoot = root;
        shopSystem = shop;
        inventory = inv;
        if (panelRoot != null)
            panelRoot.SetActive(false);

        HideBarnAnimalPurchaseUi();
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
        if (MenuStateController.Instance != null)
            MenuStateController.Instance.RegisterMenuOpen();
            
        // Activar objeto ANTES de arrancar la corrutina de animación
        gameObject.SetActive(true);
            
        if (_newBarnUi != null)
            _newBarnUi.OnOpen();
    }

    public void Close()
    {
        gameObject.SetActive(false);

        if (MenuStateController.Instance != null)
            MenuStateController.Instance.RegisterMenuClose();
    }

    public void Toggle()
    {
        if (gameObject.activeSelf) Close();
        else Open();
    }

    public bool IsOpen => gameObject.activeSelf;

    private void RefreshLabels()
    {
        // Redirigir actualización a la nueva UI
        if (_newBarnUi != null)
            _newBarnUi.RefreshData();
    }

    private void UpdateRuntimeFoodButtonLabels()
    {
        if (panelRoot == null || shopSystem == null)
            return;
        var row = panelRoot.transform.Find("RuntimeFoodRow");
        if (row == null)
            return;
        SetFoodButtonCaption(row, "BtnComidaBasica", "Pienso básico", shopSystem.GetFeedBasicCoinCost());
        SetFoodButtonCaption(row, "BtnComidaPremium", "Pienso premium", shopSystem.GetFeedPremiumCoinCost());
    }

    private static void SetFoodButtonCaption(Transform row, string childName, string label, int coins)
    {
        var t = row.Find(childName);
        if (t == null)
            return;
        var tx = t.GetComponentInChildren<Text>();
        if (tx == null)
            return;
        tx.text = coins <= 0 ? $"{label} (—)" : $"{label} ({coins} monedas)";
    }

    private static void FormatCoin(Text label, int coins, int playerCoins = 0)
    {
        if (label == null) return;
        label.text = coins <= 0 ? "—" : $"{coins} monedas";
        UIStyleSheet.ApplyCostLabel(label);
        if (coins > 0)
            UIStyleSheet.ApplyAffordableStyle(label, playerCoins >= coins);
    }

    private void RefreshCurrentResources()
    {
        if (inventory == null)
            return;

        var milk = inventory.Get(ResourceType.Milk);
        var egg = inventory.Get(ResourceType.Egg);
        var meat = inventory.Get(ResourceType.Meat);
        var coins = inventory.Get(ResourceType.Coin);
        var fb = inventory.Get(ResourceType.FeedBasic);
        var fp = inventory.Get(ResourceType.FeedPremium);

        if (currentResourcesText != null)
        {
            currentResourcesText.text =
                $"Monedas: {coins}   Comida: {fb}/{inventory.GetStorageMax(ResourceType.FeedBasic)} (+{fp})   " +
                $"Leche: {milk}/{inventory.GetStorageMax(ResourceType.Milk)}   Huevos: {egg}/{inventory.GetStorageMax(ResourceType.Egg)}   Carne: {meat}/{inventory.GetStorageMax(ResourceType.Meat)}";
            var fs = currentResourcesText.fontSize > 0 ? currentResourcesText.fontSize : 18;
            UIStyleSheet.ApplyBodyText(currentResourcesText, fs);
        }

        if (coinsOnlyText != null)
        {
            coinsOnlyText.text = $"Monedas: {coins}  Comida: {fb + fp}";
            var fs = coinsOnlyText.fontSize > 0 ? coinsOnlyText.fontSize : 20;
            UIStyleSheet.ApplyCostLabel(coinsOnlyText);
            coinsOnlyText.fontSize = fs;
        }
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
            var remove = econ.CountResourcesConsumedOnSellAll(type, n);
            var gain = econ.ComputeSellCoins(type, remove, mult);
            if (type == ResourceType.Egg)
                label.text = $"{n} {resourceName} (venden en pares) → +{gain} monedas";
            else
                label.text = $"{n} {resourceName} → +{gain} monedas";
        }

        Line(milkSellPreviewText, ResourceType.Milk, "leche");
        Line(eggSellPreviewText, ResourceType.Egg, "huevos");
        Line(meatSellPreviewText, ResourceType.Meat, "carne");

        if (sellRulesSummaryText != null)
        {
            sellRulesSummaryText.text =
                "Venta: 2 huevos = 1 moneda · 1 leche = 3 monedas · 1 carne = 2 monedas (× bonus granero si aplica).";
            var fs = sellRulesSummaryText.fontSize > 0 ? sellRulesSummaryText.fontSize : 15;
            UIStyleSheet.ApplyBodyText(sellRulesSummaryText, fs);
        }

        if (corralUpgradesSummaryText != null && shopSystem != null)
        {
            corralUpgradesSummaryText.text =
                $"Corral vacas — Almacén {shopSystem.GetCorralStorageUpgradeCoinCost(FarmAnimalKind.Cow)} monedas | " +
                $"Vida {shopSystem.GetCorralHealthUpgradeCoinCost(FarmAnimalKind.Cow)} | " +
                $"Prod. {shopSystem.GetCorralProductionUpgradeCoinCost(FarmAnimalKind.Cow)}\n" +
                $"Corral pollos — Almacén {shopSystem.GetCorralStorageUpgradeCoinCost(FarmAnimalKind.Chicken)} | " +
                $"Vida {shopSystem.GetCorralHealthUpgradeCoinCost(FarmAnimalKind.Chicken)} | " +
                $"Prod. {shopSystem.GetCorralProductionUpgradeCoinCost(FarmAnimalKind.Chicken)}\n" +
                $"Corral cerdos — Almacén {shopSystem.GetCorralStorageUpgradeCoinCost(FarmAnimalKind.Pig)} | " +
                $"Vida {shopSystem.GetCorralHealthUpgradeCoinCost(FarmAnimalKind.Pig)} | " +
                $"Prod. {shopSystem.GetCorralProductionUpgradeCoinCost(FarmAnimalKind.Pig)}";
            var fs = corralUpgradesSummaryText.fontSize > 0 ? corralUpgradesSummaryText.fontSize : 13;
            UIStyleSheet.ApplySecondaryText(corralUpgradesSummaryText, fs);
        }

        StyleSellPreviewLine(milkSellPreviewText);
        StyleSellPreviewLine(eggSellPreviewText);
        StyleSellPreviewLine(meatSellPreviewText);
    }

    private static void StyleSellPreviewLine(Text label)
    {
        if (label == null)
            return;
        var fs = label.fontSize > 0 ? label.fontSize : 14;
        UIStyleSheet.ApplySecondaryText(label, fs);
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

    private void EnsureRuntimeFoodRowOnce()
    {
        if (_didFoodRow || panelRoot == null || shopSystem == null)
            return;

        var root = panelRoot.transform;
        if (root.Find("RuntimeFoodRow") != null)
        {
            _didFoodRow = true;
            return;
        }

        var row = new GameObject("RuntimeFoodRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.layer = root.gameObject.layer;
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.SetParent(root, false);
        rowRt.anchorMin = new Vector2(0.04f, 0.055f);
        rowRt.anchorMax = new Vector2(0.96f, 0.115f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.offsetMin = Vector2.zero;
        rowRt.offsetMax = Vector2.zero;

        var h = row.GetComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.spacing = 8f;
        h.childForceExpandHeight = true;
        h.childForceExpandWidth = true;
        h.padding = new RectOffset(4, 4, 2, 2);

        CreateSellButton(row.transform, "BtnComidaBasica", "Pienso básico", OnBuyFeedBasic, false);
        CreateSellButton(row.transform, "BtnComidaPremium", "Pienso premium", OnBuyFeedPremium, true);

        _didFoodRow = true;
    }

    public void OnBuyFeedBasic()
    {
        if (shopSystem != null && shopSystem.BuyFeedBasic())
            RefreshLabels();
    }

    public void OnBuyFeedPremium()
    {
        if (shopSystem != null && shopSystem.BuyFeedPremium())
            RefreshLabels();
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

        CreateSellButton(row.transform, "BtnVenderLeche", "Vender leche", OnSellMilk, false);
        CreateSellButton(row.transform, "BtnVenderHuevos", "Vender huevos", OnSellEgg, false);
        CreateSellButton(row.transform, "BtnVenderCarne", "Vender carne", OnSellMeat, false);
        CreateSellButton(row.transform, "BtnAlmacenCorral", "Más almacén", OnBuyCorralStoragePowerUp, true);

        _didEnsureRuntimeUi = true;
    }

    private static void CreateSellButton(Transform parent, string name, string caption, UnityEngine.Events.UnityAction onClick, bool corralStorageUpgrade)
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

        var tx = textGo.GetComponent<Text>();
        tx.text = caption;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.raycastTarget = false;

        if (corralStorageUpgrade)
            UIStyleSheet.ApplyShopUpgradeAccentButton(img, tx);
        else
            UIStyleSheet.ApplySellTradeButton(img, tx);

        UIStyleSheet.ApplyButtonStates(btn);
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
        {
            cowNameText.text = c != null ? c.cowName : "Vaca";
            var fs = cowNameText.fontSize > 0 ? cowNameText.fontSize : 22;
            UIStyleSheet.ApplyPrimaryTitle(cowNameText, fs);
        }

        if (cowResourceLineText != null)
        {
            cowResourceLineText.text = c != null ? c.cowResourceLine : "Produce: leche (corral)";
            var fs = cowResourceLineText.fontSize > 0 ? cowResourceLineText.fontSize : 16;
            UIStyleSheet.ApplySecondaryText(cowResourceLineText, fs);
        }

        if (chickenNameText != null)
        {
            chickenNameText.text = c != null ? c.chickenName : "Gallina";
            var fs = chickenNameText.fontSize > 0 ? chickenNameText.fontSize : 22;
            UIStyleSheet.ApplyPrimaryTitle(chickenNameText, fs);
        }

        if (chickenResourceLineText != null)
        {
            chickenResourceLineText.text = c != null ? c.chickenResourceLine : "Produce: huevos (corral)";
            var fs = chickenResourceLineText.fontSize > 0 ? chickenResourceLineText.fontSize : 16;
            UIStyleSheet.ApplySecondaryText(chickenResourceLineText, fs);
        }

        if (pigNameText != null)
        {
            pigNameText.text = c != null ? c.pigName : "Cerdo";
            var fs = pigNameText.fontSize > 0 ? pigNameText.fontSize : 22;
            UIStyleSheet.ApplyPrimaryTitle(pigNameText, fs);
        }

        if (pigResourceLineText != null)
        {
            pigResourceLineText.text = c != null ? c.pigResourceLine : "Produce: carne (corral)";
            var fs = pigResourceLineText.fontSize > 0 ? pigResourceLineText.fontSize : 16;
            UIStyleSheet.ApplySecondaryText(pigResourceLineText, fs);
        }

        if (powerUpDescriptionTexts == null)
            return;

        for (var i = 0; i < powerUpDescriptionTexts.Length && i < 6; i++)
        {
            var t = powerUpDescriptionTexts[i];
            if (t == null) continue;
            t.text = c != null ? c.GetPowerUpDescription(i) : DefaultPowerUpDescription(i);
            var fs = t.fontSize > 0 ? t.fontSize : 15;
            UIStyleSheet.ApplySecondaryText(t, fs);
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
