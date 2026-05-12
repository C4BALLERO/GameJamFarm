using UnityEngine;

/// <summary>
/// Icono flotante sobre el corral cuando el almacén está lleno (listo para recoger).
/// Animación suave de balanceo en Y. Para progreso parcial opcional, usa <see cref="CorralStorage.FillNormalized"/> en tu UI mundo.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloatingResourceIcon : MonoBehaviour
{
    [SerializeField] private CorralStorage storage;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 2.2f;

    private Transform _iconTransform;
    private Vector3 _baseLocalPos;
    private bool _visible;

    public void Initialize(CorralStorage corralStorage, Sprite fallbackSprite)
    {
        storage = corralStorage != null ? corralStorage : GetComponent<CorralStorage>();
        var spr = fallbackSprite ?? LoadSpriteForType(storage.StoredResourceType);
        EnsureRenderer(spr);

        if (storage != null)
        {
            storage.BecameFull += Show;
            storage.CollectedOrEmptied += RefreshVisibility;
            storage.StorageChanged += OnStorageChanged;
        }

        RefreshVisibility();
    }

    private void OnStorageChanged(int current, int max)
    {
        RefreshVisibility();
    }

    private void OnDestroy()
    {
        if (storage == null)
            return;
        storage.BecameFull -= Show;
        storage.CollectedOrEmptied -= RefreshVisibility;
        storage.StorageChanged -= OnStorageChanged;
    }

    private void LateUpdate()
    {
        if (!_visible || _iconTransform == null)
            return;

        var bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        _iconTransform.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);
    }

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

    private void Show()
    {
        SetVisible(storage != null && storage.IsFull);
    }

    private void RefreshVisibility()
    {
        SetVisible(storage != null && storage.IsFull);
    }

    private void SetVisible(bool on)
    {
        _visible = on;
        if (iconRenderer != null)
            iconRenderer.enabled = on;
    }
}
