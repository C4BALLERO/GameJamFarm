using UnityEngine;

/// <summary>
/// Maps animal kinds to scene corrals (<see cref="CorralZone"/>).
/// Assign references or rely on default object names: Corral_Vacas, Corral_Pollos, Corral_Cerdos.
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralManager : MonoBehaviour
{
    public static CorralManager Instance { get; private set; }

    [Header("Direct references (preferred)")]
    [SerializeField] private CorralZone cowCorral;
    [SerializeField] private CorralZone chickenCorral;
    [SerializeField] private CorralZone pigCorral;

    [Header("Fallback names (used when references are missing)")]
    [SerializeField] private string cowCorralObjectName = "Corral_Vacas";
    [SerializeField] private string chickenCorralObjectName = "Corral_Pollos";
    [SerializeField] private string pigCorralObjectName = "Corral_Cerdos";
    [Header("Startup")]
    [SerializeField] private bool hideAnimalsAtGameStart = true;
    [SerializeField] private bool spawnStarterAnimalsAtStart = true;
    [SerializeField] [Min(0)] private int starterAnimalsPerCorral = 2;

    [Header("Visual")]
    [SerializeField] [Range(0.1f, 1f)] private float animalVisualScale = 0.5f;

    /// <summary>Fired after an animal was spawned into a corral via shop purchase.</summary>
    public event System.Action<FarmAnimalKind, GameObject> AnimalSpawnedInCorral;

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        ResolveReferencesSilent();
    }

    private void Start()
    {
        if (spawnStarterAnimalsAtStart && starterAnimalsPerCorral > 0)
            DestroyExistingAnimalsInZones();
        else if (hideAnimalsAtGameStart)
            HideExistingAnimals();

        if (spawnStarterAnimalsAtStart && starterAnimalsPerCorral > 0)
            SpawnStarterAnimals();
    }

    private void DestroyExistingAnimalsInZones()
    {
        DestroyAnimalsInZone(cowCorral);
        DestroyAnimalsInZone(chickenCorral);
        DestroyAnimalsInZone(pigCorral);
    }

    private static void DestroyAnimalsInZone(CorralZone zone)
    {
        if (zone == null)
            return;

        var animals = zone.GetComponentsInChildren<AnimalBase>(true);
        foreach (var animal in animals)
        {
            if (animal != null)
                Object.Destroy(animal.gameObject);
        }
    }

    private void SpawnStarterAnimals()
    {
        var shop = FindFirstObjectByType<ShopSystem>();
        var inv = FindFirstObjectByType<InventorySystem>();
        if (shop == null || inv == null)
        {
            Debug.LogWarning("[CorralManager] No ShopSystem o InventorySystem: no se pueden generar animales iniciales.");
            return;
        }

        for (var n = 0; n < starterAnimalsPerCorral; n++)
        {
            SpawnOne(FarmAnimalKind.Cow, shop, inv);
            SpawnOne(FarmAnimalKind.Chicken, shop, inv);
            SpawnOne(FarmAnimalKind.Pig, shop, inv);
        }
    }

    private void SpawnOne(FarmAnimalKind kind, ShopSystem shop, InventorySystem inv)
    {
        var prefab = shop.GetAnimalPrefab(kind);
        if (prefab == null)
        {
            Debug.LogWarning($"[CorralManager] Falta prefab de {kind} en ShopSystem.");
            return;
        }

        if (!TrySpawnAnimalInCorral(kind, prefab, inv, out var go) || go == null)
            return;

        go.SetActive(true);
    }

    private void ResolveReferences()
    {
        ResolveReferencesSilent();
        ValidateKinds();
    }

    private void ResolveReferencesSilent()
    {
        if (cowCorral == null && !string.IsNullOrEmpty(cowCorralObjectName))
            cowCorral = FindSceneZone(cowCorralObjectName);
        if (chickenCorral == null && !string.IsNullOrEmpty(chickenCorralObjectName))
            chickenCorral = FindSceneZone(chickenCorralObjectName);
        if (pigCorral == null && !string.IsNullOrEmpty(pigCorralObjectName))
            pigCorral = FindSceneZone(pigCorralObjectName);
    }

    private static CorralZone FindSceneZone(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go == null) return null;
        return go.GetComponent<CorralZone>();
    }

    private void ValidateKinds()
    {
        WarnMismatch(cowCorral, FarmAnimalKind.Cow);
        WarnMismatch(chickenCorral, FarmAnimalKind.Chicken);
        WarnMismatch(pigCorral, FarmAnimalKind.Pig);
    }

    private static void WarnMismatch(CorralZone zone, FarmAnimalKind expected)
    {
        if (zone == null) return;
        if (zone.AllowedKind != expected)
            Debug.LogWarning($"[CorralManager] Corral '{zone.name}' allows {zone.AllowedKind} but manager expects {expected}. Fix Allowed Kind or assignment.", zone);
    }

    public CorralZone GetZone(FarmAnimalKind kind)
    {
        return kind switch
        {
            FarmAnimalKind.Cow => cowCorral,
            FarmAnimalKind.Chicken => chickenCorral,
            _ => pigCorral
        };
    }

    /// <summary>Enemy spawn exclusion — returns true if world point lies inside any assigned corral collider.</summary>
    public bool IsPointInsideAnyCorral(Vector2 worldPoint)
    {
        return ContainsPoint(cowCorral, worldPoint)
               || ContainsPoint(chickenCorral, worldPoint)
               || ContainsPoint(pigCorral, worldPoint);
    }

    private static bool ContainsPoint(CorralZone zone, Vector2 p)
    {
        return zone != null && zone.ContainsPoint(p);
    }

    /// <summary>
    /// Instantiate prefab at random valid position inside the matching corral and wire generators/motor.
    /// </summary>
    public bool TrySpawnAnimalInCorral(FarmAnimalKind kind, GameObject prefab, InventorySystem inventory, out GameObject spawned)
    {
        spawned = null;
        if (prefab == null || inventory == null)
            return false;

        if (!PrefabMatchesKind(prefab, kind))
        {
            Debug.LogWarning($"[CorralManager] Prefab '{prefab.name}' does not match requested kind {kind}.");
            return false;
        }

        var zone = GetZone(kind);
        if (zone == null)
        {
            Debug.LogWarning($"[CorralManager] No corral assigned for {kind}. Create '{cowCorralObjectName}' / '{chickenCorralObjectName}' / '{pigCorralObjectName}' with Collider2D + CorralZone.");
            return false;
        }

        if (!zone.HasCapacity())
        {
            Debug.LogWarning($"[CorralManager] Corral '{zone.name}' is full ({zone.CurrentCount}/{zone.MaxAnimals}).");
            return false;
        }

        var pos = zone.GetRandomSpawnPosition();
        spawned = Instantiate(prefab, pos, Quaternion.identity, zone.transform);
        ApplyAnimalScale(spawned);

        if (PowerUpSystem.Instance != null && spawned.TryGetComponent<AnimalBase>(out var animal))
            animal.ApplyMaxHealthMultiplier(PowerUpSystem.Instance.AnimalHealthMultiplier);

        zone.RegisterOccupant(spawned);

        if (spawned.TryGetComponent<ResourceGenerator>(out var gen))
            gen.Init(inventory);

        if (spawned.GetComponent<FarmAnimalAudio>() == null)
            spawned.AddComponent<FarmAnimalAudio>();

        // Wander / clamp inside bounds.
        var motor = spawned.GetComponent<FarmAnimalCorralMotor>();
        if (motor == null)
            motor = spawned.AddComponent<FarmAnimalCorralMotor>();
        motor.Attach(zone);

        AnimalSpawnedInCorral?.Invoke(kind, spawned);
        return true;
    }

    private void ApplyAnimalScale(GameObject animalRoot)
    {
        if (animalRoot == null || animalVisualScale <= 0f)
            return;
        var s = animalRoot.transform.localScale;
        animalRoot.transform.localScale = new Vector3(
            s.x * animalVisualScale,
            s.y * animalVisualScale,
            s.z * animalVisualScale);
    }

    private static bool PrefabMatchesKind(GameObject prefab, FarmAnimalKind requestedKind)
    {
        if (!prefab.TryGetComponent<FarmAnimal>(out var animal))
            return true;
        return animal.Kind == requestedKind;
    }

    private void HideExistingAnimals()
    {
        HideAnimalsInZone(cowCorral);
        HideAnimalsInZone(chickenCorral);
        HideAnimalsInZone(pigCorral);
    }

    private static void HideAnimalsInZone(CorralZone zone)
    {
        if (zone == null)
            return;

        var animals = zone.GetComponentsInChildren<AnimalBase>(true);
        foreach (var animal in animals)
            animal.gameObject.SetActive(false);
    }
}
