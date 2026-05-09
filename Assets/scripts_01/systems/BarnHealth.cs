using System;
using UnityEngine;

/// <summary>
/// Vida del Granero. Crea una barra de vida en espacio mundo sobre el edificio.
/// Los enemigos pueden atacarlo como objetivo secundario.
/// </summary>
[DisallowMultipleComponent]
public sealed class BarnHealth : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private int maxHealth = 50;

    [Header("Barra de vida (espacio mundo)")]
    [SerializeField] private Vector3 barOffset   = new Vector3(0f, 1.6f, -0.5f);
    [SerializeField] private float   barWidth     = 2.0f;
    [SerializeField] private float   barHeight    = 0.18f;
    [SerializeField] private int     barSortOrder = 20;

    public event Action Died;
    public event Action<int, int> HealthChanged;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public Transform Transform => transform;

    public int GetHealth()    => CurrentHealth;
    public int GetMaxHealth() => maxHealth;

    private SpriteRenderer _fillSr;
    private Transform      _fillTf;

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        BuildWorldBar();
    }

    public void TakeDamage(int amount, Vector2 knockback)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        RefreshBar();

        if (CurrentHealth > 0) return;

        IsDead = true;
        Died?.Invoke();
        Debug.LogWarning("[BarnHealth] ¡El Granero fue destruido!");
    }

    private void RefreshBar()
    {
        if (_fillTf == null) return;
        var ratio  = Mathf.Clamp01((float)CurrentHealth / maxHealth);
        var scaleX = barWidth * ratio;

        var s = _fillTf.localScale;
        s.x = scaleX;
        _fillTf.localScale = s;

        var p = _fillTf.localPosition;
        p.x = barOffset.x - barWidth * 0.5f + scaleX * 0.5f;
        _fillTf.localPosition = p;

        if (_fillSr != null)
            _fillSr.color = Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(0.2f, 0.8f, 0.2f), ratio);
    }

    private void BuildWorldBar()
    {
        var white = MakeWhiteSprite();

        // Fondo oscuro
        var bg = new GameObject("BarnHpBg", typeof(SpriteRenderer));
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = barOffset;
        bg.transform.localScale    = new Vector3(barWidth, barHeight, 1f);
        var bgSr = bg.GetComponent<SpriteRenderer>();
        bgSr.sprite       = white;
        bgSr.color        = new Color(0.12f, 0.04f, 0.04f, 0.92f);
        bgSr.sortingOrder = barSortOrder;

        // Relleno de vida
        var fill = new GameObject("BarnHpFill", typeof(SpriteRenderer));
        fill.transform.SetParent(transform, false);
        fill.transform.localPosition = barOffset + new Vector3(0f, 0f, -0.01f);
        fill.transform.localScale    = new Vector3(barWidth, barHeight * 0.7f, 1f);
        _fillSr = fill.GetComponent<SpriteRenderer>();
        _fillSr.sprite       = white;
        _fillSr.sortingOrder = barSortOrder + 1;
        _fillTf = fill.transform;

        RefreshBar();
    }

    private static Sprite MakeWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
