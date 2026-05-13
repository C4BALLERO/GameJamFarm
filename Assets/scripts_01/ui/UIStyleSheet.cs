using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Paleta y estilo unificados «Dark Fantasy Farm» para UI generada en runtime.
/// Carga sprites desde <c>Resources/UI/</c> si existen; si no, usa texturas procedurales cacheadas.
/// </summary>
public static class UIStyleSheet
{
    // --- Paleta (Dark Fantasy Farm) ---
    public static readonly Color PanelBg = new Color32(0x1A, 0x14, 0x25, 0xF5);
    public static readonly Color PanelBorder = new Color32(0x3D, 0x2E, 0x52, 0xFF);
    public static readonly Color AccentGold = new Color32(0xD4, 0xA8, 0x47, 0xFF);
    public static readonly Color AccentRed = new Color32(0x8B, 0x3A, 0x3A, 0xFF);
    public static readonly Color AccentGreen = new Color32(0x3A, 0x7D, 0x44, 0xFF);
    public static readonly Color ButtonNormal = new Color32(0x2D, 0x20, 0x40, 0xF2);
    public static readonly Color ButtonHoverHint = new Color32(0x4A, 0x34, 0x68, 0xF2);
    public static readonly Color ButtonPressedHint = new Color32(0x1E, 0x15, 0x30, 0xF2);
    public static readonly Color TextPrimary = new Color32(0xF0, 0xE6, 0xD3, 0xFF);
    public static readonly Color TextSecondary = new Color32(0xA8, 0x9B, 0x8C, 0xFF);
    public static readonly Color TextDisabled = new Color32(0x5C, 0x54, 0x50, 0xFF);
    public static readonly Color OverlayDim = new Color32(0x0A, 0x06, 0x10, 0xD8);
    public static readonly Color ButtonClose = new Color32(0x8B, 0x3A, 0x3A, 0xF2);

    // --- HUD recursos ---
    public static readonly Color HudMilk = new Color32(0xD8, 0xE8, 0xFF, 0xFF);
    public static readonly Color HudEgg = new Color32(0xFF, 0xF0, 0xC8, 0xFF);
    public static readonly Color HudMeat = new Color32(0xFF, 0xD4, 0xD4, 0xFF);

    // --- Barras mundo ---
    public static readonly Color AllyHpHigh = new Color32(0x4C, 0xB8, 0x6A, 0xFF);
    public static readonly Color AllyHpLow = new Color32(0x1A, 0x4A, 0x28, 0xFF);
    public static readonly Color EnemyHpHigh = new Color32(0xE0, 0x58, 0x58, 0xFF);
    public static readonly Color EnemyHpLow = new Color32(0x4A, 0x18, 0x1C, 0xFF);
    public static readonly Color WorldHpBg = new Color32(0x12, 0x0E, 0x1A, 0xE8);

    private static Font _cachedFont;
    private static Sprite _whiteUnit;
    private static Sprite _panelFramed;
    private static Sprite _buttonSoft;

    public static Font GetUiFont()
    {
        if (_cachedFont != null)
            return _cachedFont;
        _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                      ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        return _cachedFont;
    }

    public static Sprite GetWhiteUnitSprite()
    {
        if (_whiteUnit != null)
            return _whiteUnit;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _whiteUnit = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _whiteUnit;
    }

    private static Sprite LoadOrNull(string resourcePath)
    {
        var s = Resources.Load<Sprite>(resourcePath);
        if (s != null)
            return s;
        var t = Resources.Load<Texture2D>(resourcePath);
        if (t == null)
            return null;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>Panel con borde sutil (sprite <c>UI/panel_dark</c> o procedural).</summary>
    public static Sprite GetPanelSprite()
    {
        var loaded = LoadOrNull("UI/panel_dark");
        if (loaded != null)
            return loaded;
        if (_panelFramed != null)
            return _panelFramed;
        _panelFramed = CreateFramedSprite(64, 64, PanelBg, PanelBorder, 4);
        return _panelFramed;
    }

    public static Sprite GetButtonSprite()
    {
        var loaded = LoadOrNull("UI/button_normal");
        if (loaded != null)
            return loaded;
        if (_buttonSoft != null)
            return _buttonSoft;
        _buttonSoft = CreateFramedSprite(48, 40, ButtonNormal, PanelBorder, 2);
        return _buttonSoft;
    }

    private static Sprite CreateFramedSprite(int w, int h, Color fill, Color edge, int border)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var isEdge = x < border || y < border || x >= w - border || y >= h - border;
                tex.SetPixel(x, y, isEdge ? edge : fill);
            }
        }

        tex.Apply();
        var borderVec = new Vector4(border, border, border, border);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borderVec);
    }

    public static void ApplyPanelImage(Image img, float alphaMul = 1f)
    {
        if (img == null)
            return;
        img.sprite = GetPanelSprite();
        img.type = Image.Type.Sliced;
        var c = PanelBg;
        c.a *= alphaMul;
        img.color = Color.white;
        img.material = null;
    }

    public static void ApplySolidPanelTint(Image img, Color tint)
    {
        if (img == null)
            return;
        img.sprite = null;
        img.type = Image.Type.Simple;
        img.color = tint;
    }

    public static void ApplyPrimaryTitle(Text t, int fontSize = 34)
    {
        if (t == null)
            return;
        t.font = GetUiFont();
        t.fontSize = fontSize;
        t.color = AccentGold;
        t.fontStyle = FontStyle.Bold;
    }

    public static void ApplyBodyText(Text t, int fontSize = 18)
    {
        if (t == null)
            return;
        t.font = GetUiFont();
        t.fontSize = fontSize;
        t.color = TextPrimary;
    }

    public static void ApplySecondaryText(Text t, int fontSize = 16)
    {
        if (t == null)
            return;
        t.font = GetUiFont();
        t.fontSize = fontSize;
        t.color = TextSecondary;
    }

    public static void ApplyDangerTitle(Text t, int fontSize = 52)
    {
        if (t == null)
            return;
        t.font = GetUiFont();
        t.fontSize = fontSize;
        t.color = AccentRed;
        t.fontStyle = FontStyle.Bold;
    }

    public static void ApplyGlowTitle(Text t)
    {
        if (t == null)
            return;
        var shadow = t.gameObject.GetComponent<Shadow>();
        if (shadow == null)
            shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.4f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    public static void SetupInputField(Image background, Text content, Text placeholder)
    {
        if (background != null)
        {
            background.sprite = GetPanelSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(ButtonPressedHint.r, ButtonPressedHint.g, ButtonPressedHint.b, 0.94f);
        }

        if (content != null)
        {
            content.font = GetUiFont();
            content.color = TextPrimary;
        }

        if (placeholder != null)
        {
            placeholder.font = GetUiFont();
            placeholder.color = TextSecondary;
        }
    }

    public static void StyleLoadingBarBackground(Image img)
    {
        if (img == null)
            return;
        img.sprite = null;
        img.color = new Color(ButtonPressedHint.r, ButtonPressedHint.g, ButtonPressedHint.b, 0.95f);
    }

    public static void StyleLoadingBarFill(Image img)
    {
        if (img == null)
            return;
        img.color = AccentGold;
    }

    public static void StyleSplashBarBackground(Image img)
    {
        StyleLoadingBarBackground(img);
    }

    public static void StyleSplashBarFill(Image img)
    {
        StyleLoadingBarFill(img);
    }

    public static void ApplyButtonGraphic(Image img, Color baseTint)
    {
        if (img == null)
            return;
        img.sprite = GetButtonSprite();
        img.type = Image.Type.Sliced;
        img.color = baseTint;
    }

    public static void ApplySellTradeButton(Image img, Text label)
    {
        ApplyButtonGraphic(img, AccentGreen);
        if (label != null)
        {
            label.font = GetUiFont();
            label.color = TextPrimary;
            label.fontSize = 13;
        }
    }

    public static void ApplyShopUpgradeAccentButton(Image img, Text label)
    {
        ApplyButtonGraphic(img, ButtonNormal);
        if (label != null)
        {
            label.font = GetUiFont();
            label.color = AccentGold;
            label.fontSize = 13;
        }
    }

    public static void ApplyGameOverPrimaryButton(Image img, Text label)
    {
        ApplyButtonGraphic(img, AccentRed);
        if (label != null)
        {
            label.font = GetUiFont();
            label.color = TextPrimary;
            label.fontSize = 22;
        }
    }

    public static void ApplyGameOverSecondaryButton(Image img, Text label)
    {
        ApplyButtonGraphic(img, ButtonNormal);
        if (label != null)
        {
            label.font = GetUiFont();
            label.color = TextPrimary;
            label.fontSize = 22;
        }
    }

    public static void ApplyMainMenuStartButton(Image img, Text label)
    {
        ApplyButtonGraphic(img, Color.Lerp(ButtonNormal, AccentGold, 0.22f));
        if (label != null)
        {
            label.font = GetUiFont();
            label.color = TextPrimary;
            label.fontStyle = FontStyle.Bold;
        }
    }

    public static void ApplyCostLabel(Text label)
    {
        if (label == null)
            return;
        label.font = GetUiFont();
        label.color = AccentGold;
    }

    public static void StylePauseMenuRoot(GameObject root)
    {
        if (root == null)
            return;
        var rootImg = root.GetComponent<Image>();
        if (rootImg != null)
        {
            ApplyPanelImage(rootImg, 0.92f);
            rootImg.color = Color.white;
        }

        foreach (var btn in root.GetComponentsInChildren<Button>(true))
        {
            var img = btn.targetGraphic as Image;
            if (img != null)
                ApplyButtonGraphic(img, ButtonNormal);
            ApplyButtonStates(btn);
            foreach (var tx in btn.GetComponentsInChildren<Text>(true))
                ApplyBodyText(tx, tx.fontSize > 0 ? tx.fontSize : 20);
        }
    }

    // ─── Nuevas utilidades visuales ───────────────────────────────────

    /// <summary>Configura ColorBlock con hover, pressed, disabled y fade duration.</summary>
    public static void ApplyButtonStates(Button btn)
    {
        if (btn == null)
            return;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.18f, 1.14f, 1.28f, 1f);
        cb.pressedColor = new Color(0.72f, 0.68f, 0.78f, 1f);
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = new Color(0.38f, 0.35f, 0.42f, 0.7f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    /// <summary>Línea horizontal decorativa dorada translúcida.</summary>
    public static GameObject CreateSeparator(Transform parent, float yNorm, float thickness = 0.004f)
    {
        var go = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.06f, yNorm);
        rt.anchorMax = new Vector2(0.94f, yNorm + thickness);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = null;
        img.color = new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.22f);
        img.raycastTarget = false;
        return go;
    }

    /// <summary>Aplica color verde (puede pagar) o rojo tenue (no puede) a una etiqueta de coste.</summary>
    public static void ApplyAffordableStyle(Text label, bool canAfford)
    {
        if (label == null)
            return;
        label.color = canAfford
            ? new Color(AccentGold.r, AccentGold.g, AccentGold.b, 1f)
            : new Color(0.72f, 0.38f, 0.36f, 0.85f);
    }

    /// <summary>Animación de escala de panel (0.85 → 1.0) con <c>unscaledDeltaTime</c>.</summary>
    public static System.Collections.IEnumerator AnimatePanelScale(RectTransform panel, float duration = 0.18f)
    {
        if (panel == null)
            yield break;
        var elapsed = 0f;
        panel.localScale = Vector3.one * 0.85f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            panel.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, t);
            yield return null;
        }
        panel.localScale = Vector3.one;
    }

    /// <summary>Pulse de texto (escala sube y baja rápido) para feedback de cambio de valor.</summary>
    public static System.Collections.IEnumerator PulseText(RectTransform textRt, float scale = 1.15f, float duration = 0.22f)
    {
        if (textRt == null)
            yield break;
        var half = duration * 0.5f;
        var elapsed = 0f;
        var original = Vector3.one;
        var peak = Vector3.one * scale;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            textRt.localScale = Vector3.Lerp(original, peak, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            textRt.localScale = Vector3.Lerp(peak, original, elapsed / half);
            yield return null;
        }
        textRt.localScale = original;
    }

    /// <summary>Crea un overlay dim de pantalla completa para modales.</summary>
    public static GameObject CreateOverlayDim(Transform parent)
    {
        var go = new GameObject("OverlayDim", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = null;
        img.color = OverlayDim;
        img.raycastTarget = true;
        return go;
    }
}
