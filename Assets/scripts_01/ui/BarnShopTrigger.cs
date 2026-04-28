using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Clic en el Granero abre la tienda (funciona con el juego en pausa usando coordenadas de pantalla).</summary>
[DisallowMultipleComponent]
public sealed class BarnShopTrigger : MonoBehaviour
{
    [SerializeField] private ShopUI shopUi;
    [SerializeField] private Collider2D clickArea;

    private Camera _cam;

    private int _openedShopFrame = -1;

    private void Awake()
    {
        if (shopUi == null)
            shopUi = FindFirstObjectByType<ShopUI>();

        if (clickArea == null)
            clickArea = GetComponent<Collider2D>();

        _cam = Camera.main;
    }

    private void Start()
    {
        if (shopUi == null)
            shopUi = FindFirstObjectByType<ShopUI>();

        if (clickArea == null)
            clickArea = GetComponent<Collider2D>();

        if (_cam == null)
            _cam = Camera.main;
    }

    /// <summary>Preferencia sobre Find cuando la escena ya tiene Granero.</summary>
    public void BindShopUiIfProvided(ShopUI ui)
    {
        shopUi = ui ?? FindFirstObjectByType<ShopUI>();
        if (clickArea == null)
            clickArea = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (_cam == null)
            _cam = Camera.main;

        if (shopUi == null)
            shopUi = FindFirstObjectByType<ShopUI>();

        if (shopUi == null || clickArea == null || _cam == null)
            return;

        if (!WasPrimaryClickDown())
            return;

        var screen = ReadPointerScreenPosition();
        var world = PointerWorldOnColliderPlane(screen);

        if (!clickArea.OverlapPoint(world))
            return;

        if (_openedShopFrame == Time.frameCount)
            return;

        shopUi.Open();
        _openedShopFrame = Time.frameCount;
    }

    /// <summary>
    /// Ray-plane intersection so clicks match <see cref="Collider2D.OverlapPoint"/> even when the camera is rotated (orthographic tilt).
    /// </summary>
    private Vector2 PointerWorldOnColliderPlane(Vector2 screenPixels)
    {
        var ray = _cam.ScreenPointToRay(screenPixels);
        var plane = new Plane(Vector3.forward, clickArea.bounds.center);

        if (plane.Raycast(ray, out var dist))
        {
            var p = ray.GetPoint(dist);
            return new Vector2(p.x, p.y);
        }

        var planeZ = clickArea.bounds.center.z;
        var d = Mathf.Abs(_cam.transform.position.z - planeZ);
        var fb = _cam.ScreenToWorldPoint(new Vector3(screenPixels.x, screenPixels.y, d));
        return new Vector2(fb.x, fb.y);
    }

    private static bool WasPrimaryClickDown()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        return m != null && m.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static Vector2 ReadPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        return m != null ? m.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
