using UnityEngine;

/// <summary>
/// Otorga recursos aleatorios al inventario cuando el enemigo muere.
/// Añadir este componente al prefab del enemigo.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyResourceDrop : MonoBehaviour
{
    [Header("Drop por muerte")]
    [SerializeField] private int minAmount = 1;
    [SerializeField] private int maxAmount = 3;
    [SerializeField] private int dropTypeCount = 2; // cuántos tipos distintos caen

    private EnemyBase _enemy;

    private void Start()
    {
        _enemy = GetComponent<EnemyBase>();
        if (_enemy != null)
            _enemy.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (_enemy != null)
            _enemy.Died -= OnDied;
    }

    private void OnDied()
    {
        var inventory = FindFirstObjectByType<InventorySystem>();
        if (inventory == null) return;

        var types = new[] { ResourceType.Milk, ResourceType.Egg, ResourceType.Meat };
        var drops = Random.Range(1, dropTypeCount + 1);
        for (var i = 0; i < drops; i++)
        {
            var type = types[Random.Range(0, types.Length)];
            var amount = Random.Range(minAmount, maxAmount + 1);
            inventory.Add(type, amount);
        }
    }
}
