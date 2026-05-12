using System.Collections;
using UnityEngine;

/// <summary>
/// Genera recursos hacia el <see cref="CorralStorage"/> del corral (no al inventario directo).
/// Usa corrutina en lugar de <c>Update</c> para no consultar cada frame.
/// </summary>
[DisallowMultipleComponent]
public sealed class ResourceGenerator : MonoBehaviour
{
    [SerializeField] private int amountPerTick = 1;
    [SerializeField] private float secondsPerTick = 6f;
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private AnimalBase animal;
    [SerializeField] private CorralStorage corralStorage;

    private Coroutine _productionRoutine;

    /// <summary>El tipo de recurso lo fija el <see cref="CorralStorage"/> del corral; el parámetro <paramref name="type"/> se conserva por compatibilidad con prefabs.</summary>
    public void Configure(ResourceType type, int amount, float seconds)
    {
        _ = type;
        amountPerTick = Mathf.Max(1, amount);
        secondsPerTick = Mathf.Max(0.25f, seconds);
        RestartProductionLoop();
    }

    public void Init(InventorySystem inv)
    {
        inventory = inv;
        RestartProductionLoop();
    }

    /// <summary>Producción hacia almacén del corral (obligatorio para economía nueva).</summary>
    public void BindCorralStorage(CorralStorage storage)
    {
        corralStorage = storage;
        RestartProductionLoop();
    }

    private void OnEnable()
    {
        RestartProductionLoop();
    }

    private void OnDisable()
    {
        StopProductionLoop();
    }

    private void Reset()
    {
        animal = GetComponent<AnimalBase>();
    }

    private void RestartProductionLoop()
    {
        StopProductionLoop();
        if (!isActiveAndEnabled || corralStorage == null)
            return;
        _productionRoutine = StartCoroutine(ProductionLoop());
    }

    private void StopProductionLoop()
    {
        if (_productionRoutine != null)
        {
            StopCoroutine(_productionRoutine);
            _productionRoutine = null;
        }
    }

    private IEnumerator ProductionLoop()
    {
        while (enabled && corralStorage != null)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen)
            {
                yield return null;
                continue;
            }

            if (animal != null && animal.IsDead)
            {
                yield return null;
                continue;
            }

            if (animal != null && animal.IsDead)
            {
                yield return null;
                continue;
            }

            var ch = corralStorage.GetComponent<CorralHealth>();
            if (ch != null && ch.IsDestroyed)
            {
                yield return null;
                continue;
            }

            var mult = PowerUpSystem.Instance != null
                ? PowerUpSystem.Instance.ResourceIntervalMultiplier
                : 1f;
            var zone = corralStorage.GetComponent<CorralZone>();
            var kind = zone != null ? zone.AllowedKind : FarmAnimalKind.Cow;
            var up = CorralUpgradeSystem.Instance != null
                ? CorralUpgradeSystem.Instance.GetProductionIntervalMultiplier(kind)
                : 1f;
            var wait = Mathf.Max(0.2f, secondsPerTick * Mathf.Max(0.02f, mult) * up);
            yield return new WaitForSeconds(wait);

            if (corralStorage != null)
                corralStorage.TryAdd(Mathf.Max(1, amountPerTick));
        }
    }
}
