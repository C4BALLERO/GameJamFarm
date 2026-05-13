using UnityEngine;
using UnityEngine.UI;

/// <summary>Muestra Leche, Huevos, Carne y Monedas en el HUD con iconos opcionales.</summary>
[DisallowMultipleComponent]
public sealed class ResourceUI : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private Text milkText;
    [SerializeField] private Text eggText;
    [SerializeField] private Text meatText;
    [SerializeField] private Text coinText;
    [Tooltip("Opcional: muestra comida total inventario (básica + premium) / máx básico.")]
    [SerializeField] private Text feedText;

    [Header("Iconos (opcional)")]
    [SerializeField] private Image milkIcon;
    [SerializeField] private Image eggIcon;
    [SerializeField] private Image meatIcon;
    [SerializeField] private Image coinIcon;

    [Header("HUD moneda automático")]
    [Tooltip("Si no hay coinText/coinIcon en la escena, crea icono + número a la derecha de la barra de recursos.")]
    [SerializeField] private bool ensureCoinHudAtRuntime = true;

    private InventorySystem _inventory;
    private bool _builtCoinHud;

    public void Bind(InventorySystem inventory)
    {
        if (inventory == null) return;
        if (_inventory != null)
            _inventory.ResourceChanged -= OnResourceChanged;

        _inventory = inventory;
        _inventory.ResourceChanged += OnResourceChanged;

        if (ensureCoinHudAtRuntime)
            TryBuildCoinHudRuntime();

        ApplyCoinSprite();
        ApplyHudPalette();
        Refresh(_inventory);
    }

    private void ApplyHudPalette()
    {
        void Line(Text t, Color c)
        {
            if (t == null)
                return;
            var fs = t.fontSize > 0 ? t.fontSize : 24;
            t.font = UIStyleSheet.GetUiFont();
            t.fontSize = fs;
            t.color = c;
        }

        Line(milkText, UIStyleSheet.HudMilk);
        Line(eggText, UIStyleSheet.HudEgg);
        Line(meatText, UIStyleSheet.HudMeat);
        Line(coinText, UIStyleSheet.AccentGold);
        Line(feedText, UIStyleSheet.AccentGreen);
    }

    /// <summary>Deja espacio a la derecha del bloque de carne y añade icono + cantidad de monedas.</summary>
    private void TryBuildCoinHudRuntime()
    {
        if (_builtCoinHud || (coinText != null && coinIcon != null))
            return;

        // Buscar container: preferir meatIcon, luego eggIcon, luego milkIcon
        var referenceIcon = meatIcon != null ? meatIcon : (eggIcon != null ? eggIcon : milkIcon);
        var referenceText = meatText != null ? meatText : (eggText != null ? eggText : milkText);
        if (referenceIcon == null || referenceText == null)
            return;

        var bar = referenceIcon.transform.parent as RectTransform;
        if (bar == null)
            return;

        _builtCoinHud = true;

        var meatRt = referenceText.rectTransform;
        var prevMax = meatRt.anchorMax;
        meatRt.anchorMax = new Vector2(Mathf.Min(prevMax.x, 0.84f), prevMax.y);

        var font = milkText != null && milkText.font != null ? milkText.font : UIStyleSheet.GetUiFont();

        var coinIconGo = new GameObject("CoinHudIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        coinIconGo.layer = bar.gameObject.layer;
        var coinRt = coinIconGo.GetComponent<RectTransform>();
        coinRt.SetParent(bar, false);
        coinRt.anchorMin = new Vector2(0.84f, 0.12f);
        coinRt.anchorMax = new Vector2(0.93f, 0.88f);
        coinRt.offsetMin = Vector2.zero;
        coinRt.offsetMax = Vector2.zero;
        coinIcon = coinIconGo.GetComponent<Image>();
        coinIcon.raycastTarget = false;
        coinIcon.preserveAspect = true;

        var coinTextGo = new GameObject("CoinHud", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        coinTextGo.layer = bar.gameObject.layer;
        var txtRt = coinTextGo.GetComponent<RectTransform>();
        txtRt.SetParent(bar, false);
        txtRt.anchorMin = new Vector2(0.92f, 0.05f);
        txtRt.anchorMax = new Vector2(0.995f, 0.95f);
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        coinText = coinTextGo.GetComponent<Text>();
        coinText.font = font;
        coinText.fontSize = milkText != null && milkText.fontSize > 0 ? milkText.fontSize : 28;
        coinText.color = UIStyleSheet.AccentGold;
        coinText.alignment = TextAnchor.MiddleLeft;
        coinText.raycastTarget = false;
        coinText.supportRichText = false;

        var barRt = bar;
        var am = barRt.anchorMax;
        barRt.anchorMax = new Vector2(Mathf.Min(0.99f, am.x + 0.08f), am.y);
    }

    private void ApplyCoinSprite()
    {
        if (coinIcon == null)
            return;
        var econ = EconomySystem.Instance != null
            ? EconomySystem.Instance
            : Object.FindFirstObjectByType<EconomySystem>();
        if (econ == null)
            return;
        var s = econ.CoinSprite;
        if (s != null)
            coinIcon.sprite = s;
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.ResourceChanged -= OnResourceChanged;
        _inventory = null;
    }

    private void OnResourceChanged(ResourceType type, int amount)
    {
        if (_inventory == null)
            return;
        switch (type)
        {
            case ResourceType.Milk:
                SetMilk(amount, _inventory.GetStorageMax(ResourceType.Milk));
                TryPulse(milkText);
                break;
            case ResourceType.Egg:
                SetEgg(amount, _inventory.GetStorageMax(ResourceType.Egg));
                TryPulse(eggText);
                break;
            case ResourceType.Meat:
                SetMeat(amount, _inventory.GetStorageMax(ResourceType.Meat));
                TryPulse(meatText);
                break;
            case ResourceType.Coin:
                SetCoin(amount);
                TryPulse(coinText);
                break;
            case ResourceType.FeedBasic:
            case ResourceType.FeedPremium:
                SetFoodHud();
                TryPulse(feedText);
                break;
        }
    }

    private void TryPulse(Text t)
    {
        if (t == null)
            return;
        StartCoroutine(UIStyleSheet.PulseText(t.rectTransform, 1.12f, 0.18f));
    }

    private void SetMilk(int v, int max)
    {
        if (milkText == null) return;
        milkText.text = milkIcon != null ? $"{v}/{max}" : $"Leche {v}/{max}";
    }

    private void SetEgg(int v, int max)
    {
        if (eggText == null) return;
        eggText.text = eggIcon != null ? $"{v}/{max}" : $"Huevos {v}/{max}";
    }

    private void SetMeat(int v, int max)
    {
        if (meatText == null) return;
        meatText.text = meatIcon != null ? $"{v}/{max}" : $"Carne {v}/{max}";
    }

    private void SetCoin(int v) { if (coinText != null) coinText.text = coinIcon != null ? $"{v}" : $"Monedas {v}"; }

    private void SetFoodHud()
    {
        if (feedText == null || _inventory == null)
            return;
        var sum = _inventory.Get(ResourceType.FeedBasic) + _inventory.Get(ResourceType.FeedPremium);
        var max = _inventory.GetStorageMax(ResourceType.FeedBasic);
        feedText.text = $"{sum}/{max}";
    }

    private void Refresh(InventorySystem inventory)
    {
        SetMilk(inventory.Get(ResourceType.Milk), inventory.GetStorageMax(ResourceType.Milk));
        SetEgg(inventory.Get(ResourceType.Egg), inventory.GetStorageMax(ResourceType.Egg));
        SetMeat(inventory.Get(ResourceType.Meat), inventory.GetStorageMax(ResourceType.Meat));
        SetCoin(inventory.Get(ResourceType.Coin));
        SetFoodHud();
        ApplyHudPalette();
    }
}
