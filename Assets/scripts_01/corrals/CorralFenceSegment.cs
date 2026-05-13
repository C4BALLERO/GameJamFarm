using System;
using UnityEngine;

/// <summary>
/// Trozo de valla sólida (2D): bloquea enemigos hasta que la rompen por contacto.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class CorralFenceSegment : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public event Action Died;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public Transform Transform => transform;

    private BoxCollider2D _box;

    private void Awake()
    {
        _box = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        CurrentHealth = Mathf.Max(1, maxHealth);
        _box.isTrigger = false;
    }

    public void Configure(int hp, Color color, int sortOrder)
    {
        maxHealth = Mathf.Max(1, hp);
        CurrentHealth = maxHealth;
        IsDead = false;

        if (_box == null)
            _box = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        _box.size = Vector2.one;
        _box.offset = Vector2.zero;
        _box.enabled = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = spriteRenderer.sprite != null ? spriteRenderer.sprite : GetOrCreateWhiteSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortOrder;
            spriteRenderer.transform.localScale = Vector3.one;
            spriteRenderer.enabled = true;
        }
    }

    public int GetHealth() => CurrentHealth;
    public int GetMaxHealth() => maxHealth;

    public void TakeDamage(int amount, Vector2 knockback)
    {
        _ = knockback;
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        if (spriteRenderer != null)
        {
            var ratio = maxHealth <= 0 ? 0f : (float)CurrentHealth / maxHealth;
            var c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.35f, 1f, ratio));
        }

        GetComponent<FenceDamageVisual>()?.OnDamaged(CurrentHealth, maxHealth);
        if (CurrentHealth > 0)
            return;

        IsDead = true;
        if (_box != null)
            _box.enabled = false;
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        Died?.Invoke();
    }

    private static Sprite _cachedWhite;

    private static Sprite GetOrCreateWhiteSprite()
    {
        if (_cachedWhite != null)
            return _cachedWhite;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _cachedWhite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _cachedWhite;
    }
}
