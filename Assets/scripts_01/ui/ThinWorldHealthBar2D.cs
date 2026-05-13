using UnityEngine;

/// <summary>
/// Franja fina de vida en espacio mundo, hija del personaje. Se actualiza en <see cref="LateUpdate"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class ThinWorldHealthBar2D : MonoBehaviour
{
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.52f, -0.02f);
    [SerializeField] private float barWidth = 0.62f;
    [SerializeField] private float barHeight = 0.05f;
    [SerializeField] private int sortingOrder = 26;

    private IDamageable _dmg;
    private Transform _fillTf;
    private SpriteRenderer _fillSr;
    private int _lastHp = int.MinValue;
    private int _lastMax = int.MinValue;
    private Color _fillHigh;
    private Color _fillLow;

    /// <summary>Crea la barra como hija de <paramref name="unit"/> (debe tener <see cref="IDamageable"/>).</summary>
    public static ThinWorldHealthBar2D Attach(Transform unit, bool enemyStrip, Vector3? localOffsetOverride = null)
    {
        if (unit == null)
            return null;

        var dmg = unit.GetComponent<IDamageable>();
        if (dmg == null)
            return null;

        var go = new GameObject("ThinWorldHpBar");
        go.transform.SetParent(unit, false);
        var bar = go.AddComponent<ThinWorldHealthBar2D>();
        if (localOffsetOverride.HasValue)
            bar.localOffset = localOffsetOverride.Value;
        go.transform.localPosition = bar.localOffset;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        bar._dmg = dmg;
        if (enemyStrip)
        {
            bar._fillHigh = UIStyleSheet.EnemyHpHigh;
            bar._fillLow = UIStyleSheet.EnemyHpLow;
            bar.BuildSprites(bar._fillHigh, UIStyleSheet.WorldHpBg);
        }
        else
        {
            bar._fillHigh = UIStyleSheet.AllyHpHigh;
            bar._fillLow = UIStyleSheet.AllyHpLow;
            bar.BuildSprites(bar._fillHigh, UIStyleSheet.WorldHpBg);
        }

        bar.RefreshVisual(true);
        return bar;
    }

    private void LateUpdate()
    {
        if (_dmg == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (_dmg.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        RefreshVisual(false);
    }

    private void RefreshVisual(bool force)
    {
        var hp = _dmg.GetHealth();
        var mx = Mathf.Max(1, _dmg.GetMaxHealth());
        if (!force && hp == _lastHp && mx == _lastMax)
            return;

        _lastHp = hp;
        _lastMax = mx;

        if (_fillTf == null)
            return;

        var ratio = mx <= 0 ? 0f : Mathf.Clamp01((float)hp / mx);
        var scaleX = barWidth * ratio;

        var s = _fillTf.localScale;
        s.x = Mathf.Max(0.02f, scaleX);
        s.y = barHeight * 0.78f;
        _fillTf.localScale = s;

        var p = _fillTf.localPosition;
        p.x = -barWidth * 0.5f + scaleX * 0.5f;
        p.y = 0f;
        p.z = -0.002f;
        _fillTf.localPosition = p;

        if (_fillSr != null)
            _fillSr.color = Color.Lerp(_fillLow, _fillHigh, ratio);
    }

    private void BuildSprites(Color fillColor, Color bgColor)
    {
        var white = CreateWhiteSprite();

        var bg = new GameObject("HpBg", typeof(SpriteRenderer));
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        var bgSr = bg.GetComponent<SpriteRenderer>();
        bgSr.sprite = white;
        bgSr.color = bgColor;
        bgSr.sortingOrder = sortingOrder;

        var fill = new GameObject("HpFill", typeof(SpriteRenderer));
        fill.transform.SetParent(transform, false);
        fill.transform.localPosition = new Vector3(0f, 0f, -0.002f);
        fill.transform.localScale = new Vector3(barWidth, barHeight * 0.78f, 1f);
        _fillSr = fill.GetComponent<SpriteRenderer>();
        _fillSr.sprite = white;
        _fillSr.color = fillColor;
        _fillSr.sortingOrder = sortingOrder + 1;
        _fillTf = fill.transform;
    }

    private static Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
