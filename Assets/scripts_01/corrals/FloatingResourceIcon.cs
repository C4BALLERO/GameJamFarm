using UnityEngine;

/// <summary>
/// Icono flotante sobre el corral según almacén: desde 5 unidades (pequeño) hasta 20+ (brillo).
/// Escala y alpha con transición suave; balanceo en Y.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloatingResourceIcon : MonoBehaviour
{
    [SerializeField] private CorralStorage storage;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private float bobAmplitude = 0.14f;
    [SerializeField] private float bobSpeed = 2.35f;
    [SerializeField] private float bounceImpulseOnIncrease = 0.08f;
    [SerializeField] private float scaleSmooth = 12f;

    private Transform _iconTransform;
    private Vector3 _baseLocalPos;
    private float _displayScale = 1f;
    private float _scaleVel;
    private float _bounceOffset;
    private int _lastAmount = -1;

    private static readonly int[] Thresholds = { 0, 5, 10, 15, 20 };

    public void Initialize(CorralStorage corralStorage, Sprite fallbackSprite)
    {
        storage = corralStorage != null ? corralStorage : GetComponent<CorralStorage>();
        var spr = fallbackSprite ?? LoadSpriteForType(storage.StoredResourceType);
        EnsureRenderer(spr);

        if (storage != null)
        {
            storage.CollectedOrEmptied += RefreshFromStorage;
            storage.StorageChanged += OnStorageChanged;
        }

        RefreshFromStorage();
    }

    private void OnStorageChanged(int current, int max) => RefreshFromStorage();

    private void OnDestroy()
    {
        if (storage == null)
            return;
        storage.CollectedOrEmptied -= RefreshFromStorage;
        storage.StorageChanged -= OnStorageChanged;
    }

    private void LateUpdate()
    {
        if (storage == null || _iconTransform == null || iconRenderer == null)
            return;

        var amount = storage.StoredAmount;
        var tier = GetTier(amount);
        if (tier <= 0)
        {
            iconRenderer.enabled = false;
            return;
        }

        if (!iconRenderer.enabled)
            iconRenderer.enabled = true;

        if (amount != _lastAmount && amount > _lastAmount)
            _bounceOffset = bounceImpulseOnIncrease;
        _lastAmount = amount;

        var targetScale = TierToScale(tier);
        var targetAlpha = TierToAlpha(tier);
        var glowPulse = tier >= 4 ? 1f + 0.07f * Mathf.Sin(Time.time * 2.6f) : 1f;

        _displayScale = Mathf.SmoothDamp(_displayScale, targetScale * glowPulse, ref _scaleVel, 1f / Mathf.Max(0.01f, scaleSmooth));
        _bounceOffset = Mathf.MoveTowards(_bounceOffset, 0f, Time.deltaTime * 0.35f);

        var bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude + _bounceOffset;
        _iconTransform.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);
        _iconTransform.localScale = Vector3.one * _displayScale;

        var c = iconRenderer.color;
        c.a = targetAlpha;
        iconRenderer.color = c;
    }

    private void RefreshFromStorage()
    {
        if (storage == null || iconRenderer == null)
            return;

        var amount = storage.StoredAmount;
        if (GetTier(amount) <= 0)
        {
            iconRenderer.enabled = false;
            _displayScale = 0.01f;
            _scaleVel = 0f;
            return;
        }

        iconRenderer.enabled = true;
    }

    private static int GetTier(int amount)
    {
        if (amount < Thresholds[1])
            return 0;
        if (amount < Thresholds[2])
            return 1;
        if (amount < Thresholds[3])
            return 2;
        if (amount < Thresholds[4])
            return 3;
        return 4;
    }

    private static float TierToScale(int tier) =>
        tier switch
        {
            1 => 0.38f,
            2 => 0.56f,
            3 => 0.78f,
            4 => 1f,
            _ => 0f
        };

    private static float TierToAlpha(int tier) =>
        tier switch
        {
            1 => 0.52f,
            2 => 0.72f,
            3 => 0.9f,
            4 => 1f,
            _ => 0f
        };

    private void EnsureRenderer(Sprite sprite)
    {
        if (iconRenderer != null)
        {
            _iconTransform = iconRenderer.transform;
            _baseLocalPos = _iconTransform.localPosition;
            if (sprite != null)
                iconRenderer.sprite = sprite;
            return;
        }

        var go = new GameObject("FloatingCollectIcon", typeof(SpriteRenderer));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localOffset;
        iconRenderer = go.GetComponent<SpriteRenderer>();
        iconRenderer.sortingOrder = 40;
        iconRenderer.sprite = sprite;
        _iconTransform = go.transform;
        _baseLocalPos = localOffset;
    }

    private static Sprite LoadSpriteForType(ResourceType type)
    {
        var name = type switch
        {
            ResourceType.Milk => "LecheGota",
            ResourceType.Egg => "HuevoGota",
            ResourceType.Meat => "CarneGota",
            _ => "LecheGota"
        };
        return Resources.Load<Sprite>(name);
    }
}
