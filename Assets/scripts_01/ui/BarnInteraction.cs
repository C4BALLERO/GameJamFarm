using UnityEngine;

/// <summary>
/// Amplía la zona clicable del granero para abrir la tienda sin frustración (BoxCollider2D más generoso).
/// Añadir al mismo GameObject que <see cref="BarnShopTrigger"/> (p. ej. desde <see cref="SceneRuntimeWiring"/>).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BarnShopTrigger))]
public sealed class BarnInteraction : MonoBehaviour
{
    [Tooltip("Multiplicador del tamaño del BoxCollider2D respecto al tamaño actual (>=1).")]
    [SerializeField] [Min(1f)] private float colliderSizeMultiplier = 1.65f;

    [Tooltip("Mínimo absoluto del collider en unidades mundo (evita cajas demasiado pequeñas).")]
    [SerializeField] private Vector2 minimumColliderSize = new(3.2f, 2.6f);

    private bool _expanded;

    private void Awake() => ApplyLargerClickArea();

    /// <summary>Idempotente: solo amplía una vez el BoxCollider2D del granero.</summary>
    public void ApplyLargerClickArea()
    {
        if (_expanded)
            return;
        if (!TryGetComponent<BoxCollider2D>(out var box))
            return;

        box.isTrigger = false;
        var s = box.size;
        s.x = Mathf.Max(minimumColliderSize.x, s.x * colliderSizeMultiplier);
        s.y = Mathf.Max(minimumColliderSize.y, s.y * colliderSizeMultiplier);
        box.size = s;
        _expanded = true;
    }
}
