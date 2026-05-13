using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates and manages the new tabbed Barn UI layout dynamically.
/// Attached automatically by ShopUI to preserve scene references.
/// </summary>
public sealed class BarnUIManager : MonoBehaviour
{
    private RectTransform _mainPanel;
    private RectTransform _contentArea;
    private CanvasGroup _contentGroup;
    
    private GameObject _animalsTab;
    private GameObject _upgradesTab;
    private GameObject _sellTab;
    
    private Button _btnTabAnimals;
    private Button _btnTabUpgrades;
    private Button _btnTabSell;
    
    private ShopSystem _shop;
    private InventorySystem _inv;
    
    private Text _feedbackText;
    
    public void Initialize(ShopSystem shop, InventorySystem inv, Transform parent)
    {
        _shop = shop;
        _inv = inv;
        
        BuildShell(parent);
        BuildTabs();
        
        // Inicializar vistas
        _animalsTab = BuildAnimalsView();
        _upgradesTab = BuildUpgradesView();
        _sellTab = BuildSellView();
        
        ShowTab(0);
    }
    
    public void RefreshData()
    {
        if (_animalsTab != null && _animalsTab.activeSelf) RefreshAnimalsView();
        if (_upgradesTab != null && _upgradesTab.activeSelf) RefreshUpgradesView();
        if (_sellTab != null && _sellTab.activeSelf) RefreshSellView();
    }
    
    public void OnOpen()
    {
        RefreshData();
        if (_mainPanel != null && _contentGroup != null)
            StartCoroutine(UIAnimationController.FadeInAndScale(_mainPanel, _contentGroup, 0.2f));
    }
    
    private void BuildShell(Transform parent)
    {
        // Panel base
        var panelGo = new GameObject("BarnUIMainPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(parent, false);
        _mainPanel = panelGo.GetComponent<RectTransform>();
        _mainPanel.anchorMin = new Vector2(0.15f, 0.1f);
        _mainPanel.anchorMax = new Vector2(0.85f, 0.9f);
        _mainPanel.offsetMin = _mainPanel.offsetMax = Vector2.zero;
        
        var img = panelGo.GetComponent<Image>();
        UIStyleSheet.ApplyPanelImage(img, 0.98f);
        
        // Título principal
        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(_mainPanel, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.05f, 0.88f);
        titleRt.anchorMax = new Vector2(0.95f, 0.98f);
        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
        var titleTx = titleGo.GetComponent<Text>();
        titleTx.text = "G R A N E R O";
        titleTx.alignment = TextAnchor.MiddleCenter;
        UIStyleSheet.ApplyPrimaryTitle(titleTx, 28);
        UIStyleSheet.ApplyGlowTitle(titleTx); // Aplicamos el glow
        
        // Separador debajo del título
        UIStyleSheet.CreateSeparator(_mainPanel, 0.87f);
        
        // Área de contenido para pestañas
        var contentGo = new GameObject("ContentArea", typeof(RectTransform), typeof(CanvasGroup));
        contentGo.transform.SetParent(_mainPanel, false);
        _contentArea = contentGo.GetComponent<RectTransform>();
        _contentArea.anchorMin = new Vector2(0.05f, 0.05f);
        _contentArea.anchorMax = new Vector2(0.95f, 0.78f);
        _contentArea.offsetMin = _contentArea.offsetMax = Vector2.zero;
        _contentGroup = contentGo.GetComponent<CanvasGroup>();
        
        // Botón Cerrar (X) estricto
        var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(_mainPanel, false);
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.92f, 0.92f);
        closeRt.anchorMax = new Vector2(0.98f, 0.98f);
        closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;
        
        var closeBtn = closeGo.GetComponent<Button>();
        UIStyleSheet.ApplyButtonGraphic(closeGo.GetComponent<Image>(), UIStyleSheet.ButtonClose);
        UIStyleSheet.ApplyButtonStates(closeBtn);
        closeBtn.onClick.AddListener(() => {
            if (MenuStateController.Instance != null) MenuStateController.Instance.RegisterMenuClose();
            GetComponent<ShopUI>().Close(); // Cerramos la UI root
        });
        
        var closeTx = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        closeTx.transform.SetParent(closeGo.transform, false);
        Stretch(closeTx.rectTransform);
        closeTx.text = "X";
        closeTx.alignment = TextAnchor.MiddleCenter;
        UIStyleSheet.ApplyBodyText(closeTx, 20);
        closeTx.color = Color.white;
        
        // Feedback Text
        var fbGo = new GameObject("Feedback", typeof(RectTransform), typeof(Text));
        fbGo.transform.SetParent(_mainPanel, false);
        var fbRt = fbGo.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0.05f, 0.01f);
        fbRt.anchorMax = new Vector2(0.95f, 0.04f);
        fbRt.offsetMin = fbRt.offsetMax = Vector2.zero;
        _feedbackText = fbGo.GetComponent<Text>();
        _feedbackText.alignment = TextAnchor.MiddleCenter;
        _feedbackText.supportRichText = true;
        UIStyleSheet.ApplySecondaryText(_feedbackText, 14);
    }
    
    private void BuildTabs()
    {
        var tabsGo = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tabsGo.transform.SetParent(_mainPanel, false);
        var tabsRt = tabsGo.GetComponent<RectTransform>();
        tabsRt.anchorMin = new Vector2(0.05f, 0.80f);
        tabsRt.anchorMax = new Vector2(0.95f, 0.86f);
        tabsRt.offsetMin = tabsRt.offsetMax = Vector2.zero;
        
        var hg = tabsGo.GetComponent<HorizontalLayoutGroup>();
        hg.spacing = 10f;
        hg.childForceExpandWidth = true;
        hg.childForceExpandHeight = true;
        
        _btnTabAnimals = CreateTabButton(tabsRt, "Animales & Comida", () => ShowTab(0));
        _btnTabUpgrades = CreateTabButton(tabsRt, "Mejoras", () => ShowTab(1));
        _btnTabSell = CreateTabButton(tabsRt, "Vender Recursos", () => ShowTab(2));
    }
    
    private Button CreateTabButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("TabBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        UIStyleSheet.ApplyButtonGraphic(img, UIStyleSheet.ButtonNormal);
        UIStyleSheet.ApplyButtonStates(btn);
        btn.onClick.AddListener(action);
        
        var tx = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        tx.transform.SetParent(go.transform, false);
        Stretch(tx.rectTransform);
        tx.text = label;
        tx.alignment = TextAnchor.MiddleCenter;
        UIStyleSheet.ApplyBodyText(tx, 16);
        
        return btn;
    }
    
    private void ShowTab(int index)
    {
        if (_animalsTab != null) _animalsTab.SetActive(index == 0);
        if (_upgradesTab != null) _upgradesTab.SetActive(index == 1);
        if (_sellTab != null) _sellTab.SetActive(index == 2);
        
        HighlightTab(_btnTabAnimals, index == 0);
        HighlightTab(_btnTabUpgrades, index == 1);
        HighlightTab(_btnTabSell, index == 2);
        
        RefreshData();
    }
    
    private void HighlightTab(Button btn, bool active)
    {
        if (btn == null) return;
        var cb = btn.colors;
        cb.normalColor = active ? new Color(1f, 0.9f, 0.6f, 1f) : Color.white; // Resaltado dorado si está activo
        btn.colors = cb;
    }
    
    // ==========================================
    // ANIMALS & FOOD VIEW
    // ==========================================
    private GameObject BuildAnimalsView()
    {
        var view = new GameObject("AnimalsView", typeof(RectTransform));
        view.transform.SetParent(_contentArea, false);
        Stretch(view.GetComponent<RectTransform>());
        
        // Food Shop at Top Left
        var foodShopGo = new GameObject("FoodShop", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        foodShopGo.transform.SetParent(view.transform, false);
        var fRt = foodShopGo.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0f, 0.7f);
        fRt.anchorMax = new Vector2(0.48f, 1f);
        fRt.offsetMin = fRt.offsetMax = Vector2.zero;
        UIStyleSheet.ApplyPanelImage(foodShopGo.GetComponent<Image>(), 0.5f);
        var fVg = foodShopGo.GetComponent<VerticalLayoutGroup>();
        fVg.padding = new RectOffset(10,10,10,10);
        fVg.spacing = 5f;
        fVg.childForceExpandHeight = false;
        
        var fTitle = new GameObject("FTitle", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        fTitle.transform.SetParent(foodShopGo.transform, false);
        fTitle.text = "\U0001F33E Comprar Pienso";
        fTitle.alignment = TextAnchor.MiddleCenter;
        UIStyleSheet.ApplyPrimaryTitle(fTitle, 18);
        
        // Food Shop buttons logic will be handled dynamically in RefreshAnimalsView
        
        // Corrals List
        var corralsGo = new GameObject("CorralsList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        corralsGo.transform.SetParent(view.transform, false);
        var cRt = corralsGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 0f);
        cRt.anchorMax = new Vector2(1f, 0.65f);
        cRt.offsetMin = cRt.offsetMax = Vector2.zero;
        var cVg = corralsGo.GetComponent<VerticalLayoutGroup>();
        cVg.spacing = 10f;
        cVg.childForceExpandHeight = false;
        
        return view;
    }
    
    private void RefreshAnimalsView()
    {
        // 1. Food Shop
        var foodShopGo = _animalsTab.transform.Find("FoodShop");
        if (foodShopGo != null)
        {
            // Limpiar botones antiguos
            foreach (Transform child in foodShopGo)
            {
                if (child.name != "FTitle") Destroy(child.gameObject);
            }
            
            var basicCost = _shop.GetFeedBasicCoinCost();
            var premCost = _shop.GetFeedPremiumCoinCost();
            var coins = _inv.Get(ResourceType.Coin);
            var basicOwned = _inv.Get(ResourceType.FeedBasic);
            var premOwned = _inv.Get(ResourceType.FeedPremium);
            
            CreateFoodBuyRow(foodShopGo, "Pienso Básico", basicCost, basicOwned, coins, () => {
                if (_shop.BuyFeedBasic()) { ShowFeedback("Pienso básico comprado", Color.green); RefreshData(); }
                else ShowFeedback("Sin monedas", Color.red);
            });
            
            CreateFoodBuyRow(foodShopGo, "Pienso Premium", premCost, premOwned, coins, () => {
                if (_shop.BuyFeedPremium()) { ShowFeedback("Pienso premium comprado", Color.green); RefreshData(); }
                else ShowFeedback("Sin monedas", Color.red);
            });
        }
        
        // 2. Corrals
        var corralsGo = _animalsTab.transform.Find("CorralsList");
        if (corralsGo != null)
        {
            foreach (Transform child in corralsGo) Destroy(child.gameObject);
            
            var cm = CorralManager.Instance;
            if (cm != null)
            {
                BuildCorralRow(corralsGo, cm.GetZone(FarmAnimalKind.Cow), FarmAnimalKind.Cow);
                BuildCorralRow(corralsGo, cm.GetZone(FarmAnimalKind.Chicken), FarmAnimalKind.Chicken);
                BuildCorralRow(corralsGo, cm.GetZone(FarmAnimalKind.Pig), FarmAnimalKind.Pig);
            }
        }
    }
    
    private void BuildCorralRow(Transform parent, CorralZone zone, FarmAnimalKind kind)
    {
        if (zone == null) return;
        var storage = zone.GetComponent<CorralStorage>();
        var food = zone.GetComponent<CorralFoodStorage>();
        var health = zone.GetComponent<CorralHealth>();
        
        var rowGo = new GameObject("Row_" + kind, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        rowGo.transform.SetParent(parent, false);
        var rt = rowGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 70f);
        UIStyleSheet.ApplyPanelImage(rowGo.GetComponent<Image>(), 0.3f);
        
        var hg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hg.padding = new RectOffset(10,10,5,5);
        hg.spacing = 10f;
        hg.childAlignment = TextAnchor.MiddleLeft;
        hg.childForceExpandWidth = false;
        
        // Info Text
        var txGo = new GameObject("Info", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        txGo.transform.SetParent(rowGo.transform, false);
        txGo.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var tx = txGo.GetComponent<Text>();
        UIStyleSheet.ApplyBodyText(tx, 14);
        
        var animalStr = kind == FarmAnimalKind.Cow ? "\U0001F404 Vacas" : kind == FarmAnimalKind.Chicken ? "\U0001F414 Gallinas" : "\U0001F43D Cerdos";
        var countStr = $"{zone.CurrentCount}/{zone.MaxAnimals}";
        var foodStr = food != null ? $"{food.CurrentFood}/{food.MaxFood}" : "—";
        var prodStr = storage != null ? $"{storage.StoredAmount}/{storage.MaxCapacity}" : "—";
        var hpStr = health != null ? $"{health.GetHealth()}/{health.GetMaxHealth()}" : "—";
        
        string statusStr = "<color=red>Starving</color>";
        if (food != null)
        {
            if (food.CurrentFood > food.MaxFood * 0.5f) statusStr = "<color=green>Well Fed</color>";
            else if (food.CurrentFood > 0) statusStr = "<color=yellow>Hungry</color>";
        }
        
        tx.supportRichText = true;
        tx.text = $"<b>{animalStr}</b> ({countStr}) | Estado: {statusStr}\n" +
                  $"Almacén: {prodStr} | Comida: {foodStr} | HP: {hpStr}";
                  
        // Comprar botón
        var cost = _shop.GetAnimalCoinPrice(kind);
        var playerCoins = _inv.Get(ResourceType.Coin);
        var btnGo = CreateButton(rowGo.transform, $"Comprar ({cost}\U0001FA99)", 140f, () => {
            bool ok = kind == FarmAnimalKind.Cow ? _shop.BuyCow() : kind == FarmAnimalKind.Chicken ? _shop.BuyChicken() : _shop.BuyPig();
            if (ok) { ShowFeedback($"{kind} comprado!", Color.green); RefreshData(); }
            else ShowFeedback("Sin monedas o lleno", Color.red);
        });
        
        var btn = btnGo.GetComponent<Button>();
        btn.interactable = playerCoins >= cost && zone.CurrentCount < zone.MaxAnimals;
    }
    
    private void CreateFoodBuyRow(Transform parent, string label, int cost, int owned, int playerCoins, UnityEngine.Events.UnityAction onClick)
    {
        var row = new GameObject("FRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var hg = row.GetComponent<HorizontalLayoutGroup>();
        hg.childAlignment = TextAnchor.MiddleLeft;
        hg.childForceExpandWidth = false;
        hg.spacing = 10f;
        
        var txGo = new GameObject("Tx", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        txGo.transform.SetParent(row.transform, false);
        txGo.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var tx = txGo.GetComponent<Text>();
        UIStyleSheet.ApplyBodyText(tx, 15);
        tx.text = $"{label} | Tienes: {owned}";
        
        var btnGo = CreateButton(row.transform, $"Comprar ({cost}\U0001FA99)", 140f, onClick);
        btnGo.GetComponent<Button>().interactable = playerCoins >= cost;
    }
    
    // ==========================================
    // UPGRADES VIEW
    // ==========================================
    private GameObject BuildUpgradesView()
    {
        var view = new GameObject("UpgradesView", typeof(RectTransform), typeof(VerticalLayoutGroup));
        view.transform.SetParent(_contentArea, false);
        Stretch(view.GetComponent<RectTransform>());
        var vg = view.GetComponent<VerticalLayoutGroup>();
        vg.spacing = 8f;
        vg.childForceExpandHeight = false;
        return view;
    }
    
    private void RefreshUpgradesView()
    {
        foreach (Transform child in _upgradesTab.transform) Destroy(child.gameObject);
        
        var coins = _inv.Get(ResourceType.Coin);
        
        // Usamos una grilla simple construida con HorizontalLayouts
        BuildUpgradeRow(_upgradesTab.transform, "Velocidad Jugador", _shop.GetSpeedUpgradeCoinCost(), coins, () => { if(_shop.BuySpeedUpgrade()) RefreshData(); });
        BuildUpgradeRow(_upgradesTab.transform, "Daño Jugador", _shop.GetAttackUpgradeCoinCost(), coins, () => { if(_shop.BuyAttackUpgrade()) RefreshData(); });
        BuildUpgradeRow(_upgradesTab.transform, "Curar Jugador", _shop.GetPlayerHealCoinCost(), coins, () => { if(_shop.BuyPlayerHealthRestore()) RefreshData(); });
        BuildUpgradeRow(_upgradesTab.transform, "Vida Animales", _shop.GetPowerUpCoinCost(1), coins, () => { if(_shop.BuyAnimalHealthPowerUp()) RefreshData(); });
        BuildUpgradeRow(_upgradesTab.transform, "Acelerar Producción", _shop.GetPowerUpCoinCost(0), coins, () => { if(_shop.BuyFasterGenerationPowerUp()) RefreshData(); });
    }
    
    private void BuildUpgradeRow(Transform parent, string label, int cost, int playerCoins, UnityEngine.Events.UnityAction onClick)
    {
        var row = new GameObject("URow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40f);
        UIStyleSheet.ApplyPanelImage(row.GetComponent<Image>(), 0.2f);
        
        var hg = row.GetComponent<HorizontalLayoutGroup>();
        hg.padding = new RectOffset(10,10,5,5);
        hg.childAlignment = TextAnchor.MiddleLeft;
        hg.childForceExpandWidth = false;
        
        var txGo = new GameObject("Tx", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        txGo.transform.SetParent(row.transform, false);
        txGo.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var tx = txGo.GetComponent<Text>();
        UIStyleSheet.ApplyBodyText(tx, 15);
        tx.text = label;
        
        var btnGo = CreateButton(row.transform, $"Mejorar ({cost}\U0001FA99)", 140f, onClick);
        btnGo.GetComponent<Button>().interactable = playerCoins >= cost;
    }
    
    // ==========================================
    // SELL VIEW
    // ==========================================
    private GameObject BuildSellView()
    {
        var view = new GameObject("SellView", typeof(RectTransform), typeof(VerticalLayoutGroup));
        view.transform.SetParent(_contentArea, false);
        Stretch(view.GetComponent<RectTransform>());
        var vg = view.GetComponent<VerticalLayoutGroup>();
        vg.spacing = 15f;
        vg.childForceExpandHeight = false;
        return view;
    }
    
    private void RefreshSellView()
    {
        foreach (Transform child in _sellTab.transform) Destroy(child.gameObject);
        
        var econ = EconomySystem.Instance;
        var sellSys = FindFirstObjectByType<SellSystem>();
        if (econ == null || sellSys == null) return;
        
        var mult = BarnUpgradeSystem.Instance != null ? BarnUpgradeSystem.Instance.SellRewardMultiplier : 1f;
        
        BuildSellRow(_sellTab.transform, ResourceType.Milk, "Leche", econ, sellSys, mult);
        BuildSellRow(_sellTab.transform, ResourceType.Egg, "Huevos", econ, sellSys, mult);
        BuildSellRow(_sellTab.transform, ResourceType.Meat, "Carne", econ, sellSys, mult);
    }
    
    private void BuildSellRow(Transform parent, ResourceType type, string label, EconomySystem econ, SellSystem sellSys, float mult)
    {
        var n = _inv.Get(type);
        var remove = econ.CountResourcesConsumedOnSellAll(type, n);
        var gain = econ.ComputeSellCoins(type, remove, mult);
        
        var row = new GameObject("SRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60f);
        UIStyleSheet.ApplyPanelImage(row.GetComponent<Image>(), 0.3f);
        
        var hg = row.GetComponent<HorizontalLayoutGroup>();
        hg.padding = new RectOffset(20,20,10,10);
        hg.childAlignment = TextAnchor.MiddleLeft;
        hg.childForceExpandWidth = false;
        
        var txGo = new GameObject("Tx", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        txGo.transform.SetParent(row.transform, false);
        txGo.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var tx = txGo.GetComponent<Text>();
        UIStyleSheet.ApplyBodyText(tx, 16);
        tx.text = $"{n} {label} disponibles\n<color=#A0FFA0>Ganancia: +{gain} \U0001FA99</color>";
        tx.supportRichText = true;
        
        var btnGo = CreateButton(row.transform, "Vender Todo", 140f, () => {
            if (sellSys.SellAll(type)) { ShowFeedback($"Vendido por {gain} monedas", Color.green); RefreshData(); }
            else ShowFeedback("Nada que vender", Color.red);
        });
        btnGo.GetComponent<Button>().interactable = gain > 0;
    }
    
    // ==========================================
    // UTILS
    // ==========================================
    private Button CreateButton(Transform parent, string label, float width, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredWidth = width;
        var img = go.GetComponent<Image>();
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        UIStyleSheet.ApplyButtonGraphic(img, UIStyleSheet.ButtonNormal);
        UIStyleSheet.ApplyButtonStates(btn);
        btn.onClick.AddListener(onClick);
        
        var tx = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        tx.transform.SetParent(go.transform, false);
        Stretch(tx.rectTransform);
        tx.text = label;
        tx.alignment = TextAnchor.MiddleCenter;
        UIStyleSheet.ApplyBodyText(tx, 14);
        
        return btn;
    }
    
    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    
    private void ShowFeedback(string msg, Color color)
    {
        if (_feedbackText != null)
        {
            _feedbackText.text = msg;
            _feedbackText.color = color;
            StopCoroutine("ClearFeedback");
            StartCoroutine(ClearFeedback());
        }
    }
    
    private System.Collections.IEnumerator ClearFeedback()
    {
        yield return new WaitForSecondsRealtime(2f);
        if (_feedbackText != null) _feedbackText.text = "";
    }
}
