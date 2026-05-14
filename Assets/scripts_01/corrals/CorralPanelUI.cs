using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Panel modal de gestión por corral: animales, comida, recursos, mejoras y compra de animal.
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralPanelUI : MonoBehaviour
{
    public static CorralPanelUI Instance { get; private set; }

    /// <summary>True si el modal de corral está visible (bloquea control del jugador).</summary>
    public static bool IsOpen => Instance != null && Instance._dim != null && Instance._dim.activeSelf;

    private Canvas _canvas;
    private GameObject _dim;
    private RectTransform _panelRt;
    private Text _title;
    private Text _body;
    private Image[] _kindStripIcons;
    private Button _btnClose;
    private Button _btnCollect;
    private Button _btnBuyAnimal;
    private Button _btnFeed;
    private Button _btnUpgStorage;
    private Button _btnUpgHealth;
    private Button _btnUpgProd;
    private Text _feedbackText;
    private Coroutine _feedbackCo;

    private CorralZone _zone;
    private CorralStorage _storage;
    private CorralFoodStorage _food;
    private CorralHealth _health;
    private ShopSystem _shop;
    private InventorySystem _inv;

    public static void EnsureExistsAndOpen(CorralZone zone)
    {
        if (zone == null)
            return;

        if (Instance == null)
        {
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogWarning("[CorralPanelUI] No se encontró Canvas en la escena.");
                return;
            }

            var root = new GameObject("CorralPanelRoot", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            var rt = root.GetComponent<RectTransform>();
            StretchFull(rt);
            Instance = root.AddComponent<CorralPanelUI>();
            Instance._canvas = canvasGo.GetComponent<Canvas>();
            Instance.BuildUi(root.transform);
        }

        Instance.Open(zone);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (_dim != null && _dim.activeSelf && GameManager.Instance != null)
            GameManager.Instance.SetCorralPanelPaused(false);

        if (Instance == this)
            Instance = null;
        if (_storage != null)
            _storage.StorageChanged -= OnStorageChanged;
        if (_food != null)
            _food.FoodChanged -= OnFoodChanged;
        if (_health != null)
            _health.HealthChanged -= OnHealthChanged;
    }

    private void LateUpdate()
    {
        if (_zone == null || !_dim.activeSelf)
            return;
        RefreshBody();
    }

    private void BuildUi(Transform parent)
    {
        _dim = new GameObject("CorralDim", typeof(RectTransform), typeof(Image), typeof(Button));
        _dim.transform.SetParent(parent, false);
        StretchFull(_dim.GetComponent<RectTransform>());
        var dimImg = _dim.GetComponent<Image>();
        UIStyleSheet.ApplySolidPanelTint(dimImg, new Color(0.04f, 0.03f, 0.08f, 0.55f));
        var dimBtn = _dim.GetComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.navigation = new Navigation { mode = Navigation.Mode.None };
        dimBtn.onClick.AddListener(Close);

        _panelRt = new GameObject("CorralPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        _panelRt.SetParent(parent, false);
        _panelRt.sizeDelta = new Vector2(560f, 620f);
        _panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRt.pivot = new Vector2(0.5f, 0.5f);
        _panelRt.anchoredPosition = Vector2.zero;
        var panelImg = _panelRt.GetComponent<Image>();
        UIStyleSheet.ApplyPanelImage(panelImg, 0.96f);

        BuildKindIconStrip(_panelRt);

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(_panelRt, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.08f, 0.745f);
        titleRt.anchorMax = new Vector2(0.92f, 0.805f);
        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
        _title = titleGo.GetComponent<Text>();
        _title.alignment = TextAnchor.MiddleCenter;
        UIStyleSheet.ApplyPrimaryTitle(_title, 22);

        var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Text));
        bodyGo.transform.SetParent(_panelRt, false);
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0.08f, 0.44f);
        bodyRt.anchorMax = new Vector2(0.92f, 0.73f);
        bodyRt.offsetMin = bodyRt.offsetMax = Vector2.zero;
        _body = bodyGo.GetComponent<Text>();
        _body.alignment = TextAnchor.UpperCenter;
        _body.supportRichText = true;
        UIStyleSheet.ApplyBodyText(_body, 15);

        // Separador visual entre estadísticas y botones
        UIStyleSheet.CreateSeparator(_panelRt, 0.425f);

        var y0 = 0.355f;
        var step = 0.078f;
        _btnCollect = CreateButton(_panelRt, "BtnCollect", Row(y0), "\u2B50 Recoger recursos", OnCollect);
        _btnBuyAnimal = CreateButton(_panelRt, "BtnBuyAnimal", Row(y0 - step), "\U0001F404 Comprar animal", OnBuyAnimal);
        _btnFeed = CreateButton(_panelRt, "BtnFeed", Row(y0 - step * 2f), "\U0001F33E Depositar 10 comida", OnDepositFeed);
        _btnUpgStorage = CreateButton(_panelRt, "BtnUpgSt", Row(y0 - step * 3f), "\U0001F4E6 Mejorar almacén", OnUpgStorage);
        _btnUpgHealth = CreateButton(_panelRt, "BtnUpgHp", Row(y0 - step * 4f), "\u2764 Mejorar vida corral", OnUpgHealth);
        _btnUpgProd = CreateButton(_panelRt, "BtnUpgPr", Row(y0 - step * 5f), "\u26A1 Mejorar producción", OnUpgProd);

        // Texto de feedback temporal
        var fbGo = new GameObject("Feedback", typeof(RectTransform), typeof(Text));
        fbGo.transform.SetParent(_panelRt, false);
        var fbRt = fbGo.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0.08f, 0.01f);
        fbRt.anchorMax = new Vector2(0.92f, 0.055f);
        fbRt.offsetMin = fbRt.offsetMax = Vector2.zero;
        _feedbackText = fbGo.GetComponent<Text>();
        _feedbackText.alignment = TextAnchor.MiddleCenter;
        _feedbackText.supportRichText = true;
        _feedbackText.text = string.Empty;
        UIStyleSheet.ApplySecondaryText(_feedbackText, 13);

        BuildCloseButtonTopRight(_panelRt);

        _dim.SetActive(false);
    }

    /// <summary>
    /// X fija en esquina superior derecha del panel (píxeles reales, por encima del resto para recibir clics).
    /// </summary>
    private void BuildCloseButtonTopRight(RectTransform panel)
    {
        var go = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(panel, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(52f, 52f);
        rt.anchoredPosition = new Vector2(-8f, -6f);

        var img = go.GetComponent<Image>();
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        UIStyleSheet.ApplyButtonGraphic(img, UIStyleSheet.AccentRed);
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(Close);

        var txGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txGo.transform.SetParent(go.transform, false);
        var trt = txGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = txGo.GetComponent<Text>();
        tx.text = "X";
        tx.alignment = TextAnchor.MiddleCenter;
        tx.fontSize = 26;
        tx.fontStyle = FontStyle.Bold;
        tx.color = UIStyleSheet.TextPrimary;
        tx.font = UIStyleSheet.GetUiFont();
        tx.raycastTarget = false;

        _btnClose = btn;
        go.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Franja superior centrada: tres iconos (vacas / gallinas / cerdos); el del corral activo queda en el centro y resaltado.
    /// </summary>
    private void BuildKindIconStrip(RectTransform panel)
    {
        var stripGo = new GameObject("KindIconStrip", typeof(RectTransform), typeof(Image));
        stripGo.transform.SetParent(panel, false);
        var stripRt = stripGo.GetComponent<RectTransform>();
        stripRt.anchorMin = new Vector2(0.08f, 0.855f);
        stripRt.anchorMax = new Vector2(0.92f, 0.98f);
        stripRt.offsetMin = stripRt.offsetMax = Vector2.zero;
        var stripBg = stripGo.GetComponent<Image>();
        stripBg.sprite = UIStyleSheet.GetPanelSprite();
        stripBg.type = Image.Type.Sliced;
        stripBg.color = new Color(0.14f, 0.11f, 0.2f, 0.55f);
        stripBg.raycastTarget = false;

        _kindStripIcons = new Image[3];
        // Tres columnas dentro de la franja; la central algo más ancha (icono principal).
        var cols = new (float minX, float maxX)[]
        {
            (0.05f, 0.30f),
            (0.34f, 0.66f),
            (0.70f, 0.95f),
        };

        for (var i = 0; i < 3; i++)
        {
            var go = new GameObject($"KindIconSlot{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(stripRt, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(cols[i].minX, 0.08f);
            rt.anchorMax = new Vector2(cols[i].maxX, 0.92f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.sprite = UIStyleSheet.GetWhiteUnitSprite();
            img.color = new Color(0.35f, 0.32f, 0.4f, 0.35f);
            go.transform.localScale = Vector3.one;
            _kindStripIcons[i] = img;
        }
    }

    private static Sprite LoadKindIconSprite(FarmAnimalKind k)
    {
        return k switch
        {
            FarmAnimalKind.Cow => Resources.Load<Sprite>("vacaIcono"),
            FarmAnimalKind.Chicken => Resources.Load<Sprite>("polloIcono"),
            _ => Resources.Load<Sprite>("cerdoIcono"),
        };
    }

    /// <summary>Ordena iconos para que la especie del corral quede centrada (estilo tienda, más claro).</summary>
    private static void ApplyKindIconStrip(Image[] slots, FarmAnimalKind active)
    {
        if (slots == null || slots.Length != 3)
            return;

        var kinds = new[] { FarmAnimalKind.Cow, FarmAnimalKind.Chicken, FarmAnimalKind.Pig };
        var idx = active == FarmAnimalKind.Cow ? 0 : active == FarmAnimalKind.Chicken ? 1 : 2;
        var leftKind = kinds[(idx + 2) % 3];
        var rightKind = kinds[(idx + 1) % 3];

        ApplyOneKindIcon(slots[0], leftKind, emphasis: false);
        ApplyOneKindIcon(slots[1], active, emphasis: true);
        ApplyOneKindIcon(slots[2], rightKind, emphasis: false);
    }

    private static void ApplyOneKindIcon(Image img, FarmAnimalKind k, bool emphasis)
    {
        if (img == null)
            return;
        var sp = LoadKindIconSprite(k);
        img.sprite = sp != null ? sp : UIStyleSheet.GetWhiteUnitSprite();
        img.color = emphasis
            ? Color.white
            : new Color(1f, 1f, 1f, 0.4f);
        var s = emphasis ? 1.08f : 0.88f;
        img.transform.localScale = Vector3.one * s;
    }

    private static (Vector2 min, Vector2 max) Row(float yNorm)
    {
        return (new Vector2(0.08f, yNorm - 0.06f), new Vector2(0.92f, yNorm + 0.02f));
    }

    private static Button CreateButton(RectTransform parent, string name, (Vector2 min, Vector2 max) anchors, string label, UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchors.min;
        rt.anchorMax = anchors.max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        UIStyleSheet.ApplyButtonGraphic(img, UIStyleSheet.ButtonNormal);
        UIStyleSheet.ApplyButtonStates(btn);
        btn.onClick.AddListener(onClick);

        var txGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txGo.transform.SetParent(go.transform, false);
        var trt = txGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = txGo.GetComponent<Text>();
        tx.text = label;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.fontSize = 15;
        UIStyleSheet.ApplyBodyText(tx, 15);
        return btn;
    }

    private void Open(CorralZone zone)
    {
        UnsubscribeOld();

        _zone = zone;
        _storage = zone != null ? zone.GetComponent<CorralStorage>() : null;
        _food = zone != null ? zone.GetComponent<CorralFoodStorage>() : null;
        _health = zone != null ? zone.GetComponent<CorralHealth>() : null;
        _shop = FindFirstObjectByType<ShopSystem>();
        _inv = FindFirstObjectByType<InventorySystem>();

        if (_storage != null)
            _storage.StorageChanged += OnStorageChanged;
        if (_food != null)
            _food.FoodChanged += OnFoodChanged;
        if (_health != null)
            _health.HealthChanged += OnHealthChanged;

        if (_canvas != null)
            _canvas.sortingOrder = Mathf.Max(_canvas.sortingOrder, 400);

        _dim.SetActive(true);
        if (_panelRt != null)
            _panelRt.gameObject.SetActive(true);

        if (MenuStateController.Instance != null)
            MenuStateController.Instance.RegisterMenuOpen();
        else if (GameManager.Instance != null)
            GameManager.Instance.SetCorralPanelPaused(true);

        if (_btnClose != null)
            _btnClose.transform.SetAsLastSibling();

        // Animación de entrada del panel
        if (_panelRt != null)
            StartCoroutine(UIStyleSheet.AnimatePanelScale(_panelRt, 0.15f));

        _title.text = KindTitle(zone.AllowedKind);
        ApplyKindIconStrip(_kindStripIcons, zone.AllowedKind);
        RefreshBody();
        RefreshButtonCosts();
    }

    private void UnsubscribeOld()
    {
        if (_storage != null)
            _storage.StorageChanged -= OnStorageChanged;
        if (_food != null)
            _food.FoodChanged -= OnFoodChanged;
        if (_health != null)
            _health.HealthChanged -= OnHealthChanged;
        _storage = null;
        _food = null;
        _health = null;
        _zone = null;
    }

    private void OnStorageChanged(int _, int __) => RefreshBody();
    private void OnFoodChanged(int _, int __) => RefreshBody();
    private void OnHealthChanged(int _, int __) => RefreshBody();

    private static string KindTitle(FarmAnimalKind k) => k switch
    {
        FarmAnimalKind.Cow => "Corral — Vacas",
        FarmAnimalKind.Chicken => "Corral — Gallinas",
        _ => "Corral — Cerdos"
    };

    private void RefreshBody()
    {
        if (_zone == null || _body == null)
            return;

        var animals = _zone.CurrentCount;
        var cap = _zone.MaxAnimals;
        var res = _storage != null ? $"{_storage.StoredAmount}/{_storage.MaxCapacity} ({_storage.StoredResourceType})" : "—";
        var food = _food != null ? $"{_food.CurrentFood}/{_food.MaxFood}" : "—";
        var hp = _health != null ? $"{_health.GetHealth()}/{_health.GetMaxHealth()}" : "—";
        var invFeed = _inv != null ? _inv.Get(ResourceType.FeedBasic) + _inv.Get(ResourceType.FeedPremium) : 0;

        _body.text =
            "<b>Estado del corral</b>\n\n" +
            $"\U0001F404 Animales \u2022 {animals} / {cap}\n" +
            $"\U0001F4E6 Almacén \u2022 {res}\n" +
            $"\U0001F33E Comida corral \u2022 {food}\n" +
            $"\u2764 Vida \u2022 {hp}\n" +
            $"\U0001F3E0 Comida inventario \u2022 {invFeed}\n";

        RefreshButtonCosts();
    }

    private void RefreshButtonCosts()
    {
        if (_shop == null || _zone == null)
            return;
        var k = _zone.AllowedKind;
        var coins = _inv != null ? _inv.Get(ResourceType.Coin) : 0;
        SetButtonCost(_btnUpgStorage, "\U0001F4E6 Mejorar almacén", _shop.GetCorralStorageUpgradeCoinCost(k), coins);
        SetButtonCost(_btnUpgHealth, "\u2764 Mejorar vida", _shop.GetCorralHealthUpgradeCoinCost(k), coins);
        SetButtonCost(_btnUpgProd, "\u26A1 Mejorar producción", _shop.GetCorralProductionUpgradeCoinCost(k), coins);
        SetButtonCost(_btnBuyAnimal, "\U0001F404 Comprar animal", _shop.GetAnimalCoinPrice(k), coins);

        // Recoger: solo si hay stock
        if (_btnCollect != null)
        {
            var hasStock = _storage != null && _storage.StoredAmount > 0;
            _btnCollect.interactable = hasStock;
        }

        // Comida: solo si hay en inventario
        if (_btnFeed != null)
        {
            var hasFeed = _inv != null && (_inv.Get(ResourceType.FeedBasic) > 0 || _inv.Get(ResourceType.FeedPremium) > 0);
            var hasRoom = _food != null && CorralCapacityValidator.HasRoomForFood(_food, 1);
            _btnFeed.interactable = hasFeed && hasRoom;
        }
    }

    private static void SetButtonCost(Button btn, string label, int coins, int playerCoins)
    {
        if (btn == null)
            return;
        var tx = btn.GetComponentInChildren<Text>();
        if (tx == null)
            return;
        if (coins <= 0)
        {
            tx.text = $"{label} (—)";
            btn.interactable = false;
        }
        else
        {
            var canAfford = playerCoins >= coins;
            tx.text = canAfford ? $"{label} ({coins} \U0001FA99)" : $"{label} ({coins} \U0001FA99 \u2014 sin monedas)";
            btn.interactable = canAfford;
            UIStyleSheet.ApplyAffordableStyle(tx, canAfford);
        }
    }

    public void Close()
    {
        if (MenuStateController.Instance != null)
            MenuStateController.Instance.RegisterMenuClose();
        else if (GameManager.Instance != null)
            GameManager.Instance.SetCorralPanelPaused(false);

        _dim.SetActive(false);
        if (_panelRt != null)
            _panelRt.gameObject.SetActive(false);
        UnsubscribeOld();
    }

    private void ShowFeedback(string msg, Color color)
    {
        if (_feedbackText == null)
            return;
        _feedbackText.text = msg;
        _feedbackText.color = color;
        if (_feedbackCo != null)
        {
            StopCoroutine(_feedbackCo);
            _feedbackCo = null;
        }

        _feedbackCo = StartCoroutine(ClearFeedbackAfterDelay());
    }

    private IEnumerator ClearFeedbackAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1.8f);
        if (_feedbackText != null)
            _feedbackText.text = string.Empty;
        _feedbackCo = null;
    }

    private void OnCollect()
    {
        if (_storage == null || _inv == null)
            return;
        var prev = _storage.StoredAmount;
        _storage.CollectAllTo(_inv);
        var collected = prev - (_storage != null ? _storage.StoredAmount : 0);
        if (collected > 0)
            ShowFeedback($"\u2705 +{collected} recursos recogidos", UIStyleSheet.AccentGreen);
        else
            ShowFeedback("\u26A0 Nada que recoger", UIStyleSheet.TextSecondary);
        RefreshBody();
    }

    private void OnBuyAnimal()
    {
        if (_shop == null || _zone == null || _inv == null)
            return;
        var ok = _zone.AllowedKind switch
        {
            FarmAnimalKind.Cow => _shop.BuyCow(),
            FarmAnimalKind.Chicken => _shop.BuyChicken(),
            _ => _shop.BuyPig()
        };
        if (ok)
        {
            ShowFeedback("\u2705 ¡Animal comprado!", UIStyleSheet.AccentGreen);
            RefreshBody();
        }
        else
            ShowFeedback("\u274C No se pudo comprar", UIStyleSheet.AccentRed);
    }

    private void OnDepositFeed()
    {
        if (_food == null || _inv == null)
            return;

        if (!CorralCapacityValidator.HasRoomForFood(_food, 1))
        {
            ShowFeedback(CorralCapacityValidator.CorralFoodFullMessage, UIStyleSheet.AccentRed);
            RefreshBody();
            return;
        }

        const int want = 10;
        const int premiumFoodPerUnit = 2;

        if (CorralFoodSystem.TryDepositBasic(_food, _inv, want, out _, out var toCorralB) && toCorralB > 0)
        {
            ShowFeedback($"\u2705 +{toCorralB} comida depositada", UIStyleSheet.AccentGreen);
            RefreshBody();
            return;
        }

        if (CorralFoodSystem.TryDepositPremium(_food, _inv, want, premiumFoodPerUnit, out _, out var toCorralP) && toCorralP > 0)
        {
            ShowFeedback($"\u2705 +{toCorralP} comida (premium)", UIStyleSheet.AccentGold);
            RefreshBody();
            return;
        }

        if ((_inv.Get(ResourceType.FeedBasic) > 0 || _inv.Get(ResourceType.FeedPremium) > 0) &&
            !CorralCapacityValidator.HasRoomForFood(_food, 1))
            ShowFeedback(CorralCapacityValidator.CorralFoodFullMessage, UIStyleSheet.AccentRed);
        else
            ShowFeedback("\u26A0 Sin comida en inventario", UIStyleSheet.TextSecondary);

        RefreshBody();
    }

    private void OnUpgStorage()
    {
        if (_shop == null || _zone == null)
            return;
        if (_shop.BuyCorralStorageUpgrade(_zone.AllowedKind))
        {
            ShowFeedback("\u2705 ¡Almacén mejorado!", UIStyleSheet.AccentGold);
            RefreshBody();
        }
        else
            ShowFeedback("\u274C Sin monedas suficientes", UIStyleSheet.AccentRed);
    }

    private void OnUpgHealth()
    {
        if (_shop == null || _zone == null)
            return;
        if (_shop.BuyCorralHealthUpgrade(_zone.AllowedKind))
        {
            ShowFeedback("\u2705 ¡Vida del corral mejorada!", UIStyleSheet.AccentGold);
            RefreshBody();
        }
        else
            ShowFeedback("\u274C Sin monedas suficientes", UIStyleSheet.AccentRed);
    }

    private void OnUpgProd()
    {
        if (_shop == null || _zone == null)
            return;
        if (_shop.BuyCorralProductionUpgrade(_zone.AllowedKind))
        {
            ShowFeedback("\u2705 ¡Producción mejorada!", UIStyleSheet.AccentGold);
            RefreshBody();
        }
        else
            ShowFeedback("\u274C Sin monedas suficientes", UIStyleSheet.AccentRed);
    }
}
