using System;
using UnityEngine;

/// <summary>
/// Vida del corral (objetivo prioritario de enemigos). Estados: sano → dañado → destruido.
/// Barra mundo solo cuando ha recibido daño y aún no está destruido.
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralHealth : MonoBehaviour, IDamageable
{
    public enum CorralState
    {
        Healthy,
        Damaged,
        Destroyed
    }

    [Header("Refs")]
    [SerializeField] private CorralZone zone;

    [Header("Vida")]
    [SerializeField] private int baseMaxHealth = 45;

    [Header("Barra mundo (solo visible si dañado)")]
    [SerializeField] private Vector3 barOffset = new Vector3(0f, 1.35f, -0.5f);
    [SerializeField] private float barWidth = 2.2f;
    [SerializeField] private float barHeight = 0.16f;
    [SerializeField] private int barSortOrder = 18;

    [Header("Destruido — oscurecer sprites del corral")]
    [SerializeField] [Range(0.2f, 1f)] private float destroyedTint = 0.45f;

    public event Action Died;
    public event Action<int, int> HealthChanged;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsDestroyed => IsDead;
    public Transform Transform => transform;

    public CorralState State
    {
        get
        {
            if (IsDestroyed)
                return CorralState.Destroyed;
            if (CurrentHealth < GetMaxHealth())
                return CorralState.Damaged;
            return CorralState.Healthy;
        }
    }

    private GameObject _barRoot;
    private SpriteRenderer _fillSr;
    private Transform _fillTf;
    private int _runtimeMax;
    private bool _initializedFromManager;

    private void Awake()
    {
        if (zone == null)
            zone = GetComponent<CorralZone>();
        RecalculateMaxHealth();
        CurrentHealth = Mathf.Max(1, _runtimeMax);
        BuildWorldBar();
        SetBarVisible(false);
    }

    private void Start()
    {
        EnsureDamageTriggerIfNeeded();
    }

    public void InitializeFromZone(CorralZone corralZone)
    {
        zone = corralZone != null ? corralZone : GetComponent<CorralZone>();
        RecalculateMaxHealth();

        if (!_initializedFromManager)
        {
            _initializedFromManager = true;
            CurrentHealth = Mathf.Max(1, _runtimeMax);
        }
        else if (!IsDead)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth, 1, _runtimeMax);
        }

        RefreshBarVisual();
        HealthChanged?.Invoke(CurrentHealth, _runtimeMax);
    }

    /// <summary>Llamar tras comprar mejoras de vida de corral.</summary>
    public void RecalculateMaxHealth()
    {
        var bonus = 0;
        if (zone != null && CorralUpgradeSystem.Instance != null)
            bonus = CorralUpgradeSystem.Instance.GetExtraCorralMaxHealth(zone.AllowedKind);

        _runtimeMax = Mathf.Max(1, baseMaxHealth + bonus);
        CurrentHealth = Mathf.Min(CurrentHealth, _runtimeMax);
        if (CurrentHealth <= 0 && !IsDead)
            CurrentHealth = 1;
        RefreshBarVisual();
    }

    public int GetHealth() => CurrentHealth;
    public int GetMaxHealth() => _runtimeMax;

    public void TakeDamage(int amount, Vector2 knockback)
    {
        _ = knockback;
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, _runtimeMax);
        RefreshBarVisual();
        SetBarVisible(CurrentHealth < _runtimeMax && CurrentHealth > 0);

        if (CurrentHealth > 0)
            return;

        IsDead = true;
        SetBarVisible(false);
        ApplyDestroyedVisuals();
        Died?.Invoke();
        Debug.LogWarning($"[CorralHealth] Corral destruido: {name}");
    }

    private void SetBarVisible(bool on)
    {
        if (_barRoot != null)
            _barRoot.SetActive(on);
    }

    private void RefreshBarVisual()
    {
        if (_fillTf == null)
            return;

        var ratio = _runtimeMax <= 0 ? 0f : Mathf.Clamp01((float)CurrentHealth / _runtimeMax);
        var scaleX = barWidth * ratio;

        var s = _fillTf.localScale;
        s.x = scaleX;
        _fillTf.localScale = s;

        var p = _fillTf.localPosition;
        p.x = barOffset.x - barWidth * 0.5f + scaleX * 0.5f;
        _fillTf.localPosition = p;

        if (_fillSr != null)
            _fillSr.color = Color.Lerp(new Color(0.85f, 0.12f, 0.15f), new Color(0.25f, 0.65f, 0.28f), ratio);
    }

    private void BuildWorldBar()
    {
        _barRoot = new GameObject("CorralHpBar");
        _barRoot.transform.SetParent(transform, false);
        _barRoot.transform.localPosition = Vector3.zero;
        _barRoot.SetActive(false);

        var white = MakeWhiteSprite();

        var bg = new GameObject("Bg", typeof(SpriteRenderer));
        bg.transform.SetParent(_barRoot.transform, false);
        bg.transform.localPosition = barOffset;
        bg.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        var bgSr = bg.GetComponent<SpriteRenderer>();
        bgSr.sprite = white;
        bgSr.color = new Color(0.08f, 0.05f, 0.1f, 0.9f);
        bgSr.sortingOrder = barSortOrder;

        var fill = new GameObject("Fill", typeof(SpriteRenderer));
        fill.transform.SetParent(_barRoot.transform, false);
        fill.transform.localPosition = barOffset + new Vector3(0f, 0f, -0.01f);
        fill.transform.localScale = new Vector3(barWidth, barHeight * 0.72f, 1f);
        _fillSr = fill.GetComponent<SpriteRenderer>();
        _fillSr.sprite = white;
        _fillSr.sortingOrder = barSortOrder + 1;
        _fillTf = fill.transform;

        RefreshBarVisual();
    }

    private void ApplyDestroyedVisuals()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null)
                continue;
            if (_barRoot != null && sr.transform.IsChildOf(_barRoot.transform))
                continue;
            var c = sr.color;
            sr.color = new Color(c.r * destroyedTint, c.g * destroyedTint, c.b * destroyedTint, c.a);
        }
    }

    private void EnsureDamageTriggerIfNeeded()
    {
        if (zone == null)
            zone = GetComponent<CorralZone>();

        var col = zone != null ? zone.AreaCollider : GetComponent<Collider2D>();
        if (col == null)
            return;

        var hit = transform.Find("CorralDamageHitbox");
        if (hit != null)
            return;

        var go = new GameObject("CorralDamageHitbox");
        go.transform.SetParent(transform, false);
        go.layer = gameObject.layer;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        var b = col.bounds;
        var centerLocal = transform.InverseTransformPoint(b.center);
        var extWorld = b.extents;
        var halfLocalX = Mathf.Abs(transform.InverseTransformVector(new Vector3(extWorld.x, 0f, 0f)).x);
        var halfLocalY = Mathf.Abs(transform.InverseTransformVector(new Vector3(0f, extWorld.y, 0f)).y);
        box.offset = new Vector2(centerLocal.x, centerLocal.y);
        box.size = new Vector2(Mathf.Max(0.5f, halfLocalX * 2f), Mathf.Max(0.5f, halfLocalY * 2f));

        go.AddComponent<CorralDamageRelay>();
    }

    private static Sprite MakeWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
