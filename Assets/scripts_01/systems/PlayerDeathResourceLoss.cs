using UnityEngine;

/// <summary>
/// Borra todos los recursos del inventario cuando el jugador muere.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerDeathResourceLoss : MonoBehaviour
{
    private PlayerHealth    _health;
    private InventorySystem _inventory;

    private void Start()
    {
        _inventory = FindFirstObjectByType<InventorySystem>();

        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            _health = player.GetComponent<PlayerHealth>();

        if (_health == null)
            _health = FindFirstObjectByType<PlayerHealth>();

        if (_health != null)
            _health.Died += OnPlayerDied;
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.Died -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        _inventory?.ClearAll();
    }
}
