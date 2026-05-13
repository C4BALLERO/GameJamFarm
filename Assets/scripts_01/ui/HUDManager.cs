using UnityEngine;

/// <summary>
/// Opcional: asegura que el HUD de recursos esté enlazado al inventario (por si <see cref="UIManager"/> no corre antes).
/// </summary>
[DisallowMultipleComponent]
public sealed class HUDManager : MonoBehaviour
{
    [SerializeField] private ResourceUI resourceUi;
    [SerializeField] private InventorySystem inventory;

    private bool _bound;

    private void Start()
    {
        TryBind();
    }

    private void LateUpdate()
    {
        if (_bound)
            return;
        TryBind();
    }

    private void TryBind()
    {
        if (_bound)
            return;
        if (resourceUi == null)
            resourceUi = FindFirstObjectByType<ResourceUI>();
        if (inventory == null)
            inventory = FindFirstObjectByType<InventorySystem>();
        if (resourceUi != null && inventory != null)
        {
            resourceUi.Bind(inventory);
            _bound = true;
        }
    }
}
