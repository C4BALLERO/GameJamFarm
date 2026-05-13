using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lógica de recolección: clic dentro de <see cref="CorralZone"/> transfiere
/// <see cref="CorralStorage"/> al <see cref="InventorySystem"/>. No usa inventario hasta el clic.
/// </summary>
[DisallowMultipleComponent]
public sealed class ResourceCollector : MonoBehaviour
{
    [SerializeField] private CorralZone zone;
    [SerializeField] private CorralStorage storage;
    [SerializeField] private InventorySystem inventory;

    private Camera _cam;

    private void Awake()
    {
        if (zone == null)
            zone = GetComponent<CorralZone>();
        if (storage == null)
            storage = GetComponent<CorralStorage>();
        _cam = Camera.main;
    }

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<InventorySystem>();
    }

    /// <summary>Enlace explícito desde <see cref="CorralManager"/> / <see cref="CorralCollectTrigger"/>.</summary>
    public void Configure(CorralZone corralZone, CorralStorage corralStorage, InventorySystem inv)
    {
        zone = corralZone != null ? corralZone : GetComponent<CorralZone>();
        storage = corralStorage != null ? corralStorage : GetComponent<CorralStorage>();
        inventory = inv;
    }

    /// <summary>
    /// Un fotograma de entrada: si el jugador acaba de pulsar dentro del corral y hay stock, recoge.
    /// </summary>
    /// <returns><c>true</c> si se transfirió al menos una unidad al inventario.</returns>
    public bool TryProcessInputThisFrame()
    {
        if (GetComponent<CorralPanelOpener>() != null)
            return false;
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen)
            return false;
        if (zone == null || storage == null || inventory == null)
            return false;

        var ch = zone.GetComponent<CorralHealth>();
        if (ch != null && ch.IsDestroyed)
            return false;

        if (!WasPrimaryClickDown())
            return false;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (_cam == null)
            _cam = Camera.main;
        if (_cam == null)
            return false;

        var world = PointerWorldOnPlane();
        if (!zone.ContainsPoint(world))
            return false;

        if (storage.StoredAmount <= 0)
            return false;

        var health = zone != null ? zone.GetComponent<CorralHealth>() : GetComponent<CorralHealth>();
        if (health != null && health.IsDestroyed)
            return false;

        var moved = storage.CollectAllTo(inventory);
        return moved > 0;
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

    private Vector2 PointerWorldOnPlane()
    {
#if ENABLE_INPUT_SYSTEM
        var screen = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        var screen = (Vector2)Input.mousePosition;
#endif
        if (_cam == null)
            return Vector2.zero;

        var ray = _cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.forward, transform.position);
        if (plane.Raycast(ray, out var dist))
        {
            var p = ray.GetPoint(dist);
            return new Vector2(p.x, p.y);
        }

        var z = Mathf.Abs(_cam.transform.position.z - transform.position.z);
        var w = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
        return new Vector2(w.x, w.y);
    }
}
