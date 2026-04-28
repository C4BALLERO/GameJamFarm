using UnityEngine;

/// <summary>
/// Finalizes references that are awkward to serialize across prefabs (inventory for animals, spawn player ref, UI bind).
/// Runs after other <see cref="MonoBehaviour.Start"/> calls by default execution order.
/// </summary>
[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
public sealed class SceneRuntimeWiring : MonoBehaviour
{
    private void Start()
    {
        var inv = FindFirstObjectByType<InventorySystem>();
        foreach (var gen in FindObjectsByType<ResourceGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (inv != null)
                gen.Init(inv);
        }

        var shop = FindFirstObjectByType<ShopSystem>();
        if (shop != null && inv != null)
            shop.Bind(inv);

        var spawner = FindFirstObjectByType<SpawnManager>();
        var player = FindFirstObjectByType<PlayerController>();
        if (spawner != null && player != null)
            spawner.SetPlayer(player.transform);
        if (player != null && player.TryGetComponent<PlayerHealth>(out var playerHealth))
            playerHealth.SetRespawnPoint(player.transform.position);

        if (player != null && Camera.main != null)
        {
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            if (follow == null)
                follow = Camera.main.gameObject.AddComponent<CameraFollow2D>();
            follow.SetTarget(player.transform);
        }

        var tm = FindFirstObjectByType<TimeManager>();
        if (spawner != null && tm != null)
            spawner.SetTimeManager(tm);

        var ui = FindFirstObjectByType<UIManager>();
        if (ui != null && inv != null && player != null && player.TryGetComponent<PlayerHealth>(out var ph))
            ui.Bind(ph, inv);
    }
}
