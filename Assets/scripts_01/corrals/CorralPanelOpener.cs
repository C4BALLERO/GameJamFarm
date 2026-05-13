using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Abre <see cref="CorralPanelUI"/> al hacer clic dentro del <see cref="CorralZone"/> (prioridad sobre recolección directa).
/// </summary>
[DefaultExecutionOrder(-250)]
[DisallowMultipleComponent]
public sealed class CorralPanelOpener : MonoBehaviour
{
    private CorralZone _zone;
    private Camera _cam;

    private void Awake()
    {
        _zone = GetComponent<CorralZone>();
        _cam = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen)
            return;
        if (_zone == null)
            return;
        if (!WasPrimaryClickDown())
            return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (_cam == null)
            _cam = Camera.main;
        if (_cam == null)
            return;

        var world = PointerWorldOnPlane();
        if (!_zone.ContainsPoint(world))
            return;

        CorralPanelUI.EnsureExistsAndOpen(_zone);
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
