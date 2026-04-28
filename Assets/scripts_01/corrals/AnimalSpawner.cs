using UnityEngine;

/// <summary>
/// Shop-facing facade that delegates spawning to <see cref="CorralManager"/>.
/// Keeps shop logic separate from zone bookkeeping.
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimalSpawner : MonoBehaviour
{
    [SerializeField] private CorralManager corralManager;

    /// <summary>Fired after a purchased animal GameObject exists and is wired.</summary>
    public event System.Action<FarmAnimalKind, GameObject> AnimalSpawned;

    private void Awake()
    {
        if (corralManager == null)
            corralManager = FindFirstObjectByType<CorralManager>();
    }

    /// <summary>Spawn purchased livestock inside the proper corral.</summary>
    public bool TrySpawnPurchasedAnimal(FarmAnimalKind kind, GameObject prefab, InventorySystem inventory, out GameObject spawned)
    {
        spawned = null;
        if (corralManager == null)
            corralManager = FindFirstObjectByType<CorralManager>();

        if (corralManager == null)
        {
            Debug.LogError("[AnimalSpawner] CorralManager missing in scene.");
            return false;
        }

        if (!corralManager.TrySpawnAnimalInCorral(kind, prefab, inventory, out spawned))
            return false;

        AnimalSpawned?.Invoke(kind, spawned);
        return true;
    }
}
