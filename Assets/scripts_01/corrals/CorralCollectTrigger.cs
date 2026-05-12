using UnityEngine;

/// <summary>
/// Delgado: orden de ejecución temprano y delegación en <see cref="ResourceCollector"/>.
/// El clic dentro del corral vacía <see cref="CorralStorage"/> hacia el inventario.
/// </summary>
[DefaultExecutionOrder(-110)]
[RequireComponent(typeof(ResourceCollector))]
[DisallowMultipleComponent]
public sealed class CorralCollectTrigger : MonoBehaviour
{
    private ResourceCollector _collector;

    private void Awake()
    {
        _collector = GetComponent<ResourceCollector>();
        if (_collector == null)
            _collector = gameObject.AddComponent<ResourceCollector>();
    }

    /// <summary>Enlaza desde <see cref="CorralManager"/> al crear componentes de economía.</summary>
    public void Initialize(CorralZone corralZone, CorralStorage corralStorage, InventorySystem inv)
    {
        if (_collector == null)
            _collector = GetComponent<ResourceCollector>() ?? gameObject.AddComponent<ResourceCollector>();
        _collector.Configure(corralZone, corralStorage, inv);
    }

    private void Update()
    {
        _collector.TryProcessInputThisFrame();
    }
}
