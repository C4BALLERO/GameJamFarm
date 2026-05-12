using UnityEngine;

/// <summary>
/// Applies barn-purchased power-ups that affect resource rate, animals, player stats and enemy spawn pacing.
/// </summary>
[DisallowMultipleComponent]
public sealed class PowerUpSystem : MonoBehaviour
{
    [Header("Levels")]
    [SerializeField] private int resourceBoostLevel;
    [SerializeField] private int animalHealthLevel;
    [SerializeField] private int playerDamageLevel;
    [SerializeField] private int playerMoveLevel;
    [SerializeField] private int spawnDelayReductionLevel;

    [Header("Per Level Effects")]
    [SerializeField] [Range(0.02f, 1f)] private float resourceIntervalMultiplierPerLevel = 0.9f;
    [SerializeField] [Range(0f, 1f)] private float animalHealthBonusPerLevel = 0.2f;
    [SerializeField] private int playerDamageTiersPerPurchase = 1;
    [SerializeField] private int playerMoveTiersPerPurchase = 1;
    [SerializeField] [Range(0.02f, 1f)] private float spawnIntervalMultiplierPerLevel = 0.9f;

    [Header("Almacén en corral")]
    [SerializeField] private int bonusCorralStorageSlotsPerLevel = 4;

    private int _corralStorageLevel;

    public static PowerUpSystem Instance { get; private set; }

    public float ResourceIntervalMultiplier =>
        Mathf.Pow(Mathf.Clamp(resourceIntervalMultiplierPerLevel, 0.02f, 1f), Mathf.Max(0, resourceBoostLevel));

    public float AnimalHealthMultiplier => 1f + Mathf.Max(0, animalHealthLevel) * Mathf.Max(0f, animalHealthBonusPerLevel);

    public float EnemySpawnIntervalMultiplier =>
        Mathf.Pow(Mathf.Clamp(spawnIntervalMultiplierPerLevel, 0.02f, 1f), Mathf.Max(0, spawnDelayReductionLevel));

    /// <summary>Capacidad extra por nivel del power-up de almacén (suma al máximo base del corral).</summary>
    public int BonusCorralStorageCapacity => Mathf.Max(0, _corralStorageLevel) * Mathf.Max(0, bonusCorralStorageSlotsPerLevel);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void BuyResourceGenerationBoost()
    {
        resourceBoostLevel++;
    }

    public void BuyAnimalHealthBoost()
    {
        animalHealthLevel++;
        var allAnimals = FindObjectsByType<AnimalBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var animal in allAnimals)
            animal.ApplyMaxHealthMultiplier(1f + Mathf.Max(0f, animalHealthBonusPerLevel));
    }

    public void BuyPlayerDamageBoost()
    {
        playerDamageLevel++;
        var combat = FindFirstObjectByType<PlayerCombat>();
        if (combat == null)
            return;

        var times = Mathf.Max(1, playerDamageTiersPerPurchase);
        for (var i = 0; i < times; i++)
            combat.IncrementAttackTier();
    }

    public void BuyPlayerMoveBoost()
    {
        playerMoveLevel++;
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null)
            return;

        var times = Mathf.Max(1, playerMoveTiersPerPurchase);
        for (var i = 0; i < times; i++)
            player.IncrementSpeedTier();
    }

    public void BuySpawnDelayReductionBoost()
    {
        spawnDelayReductionLevel++;
    }

    public void BuyCorralStorageBoost()
    {
        _corralStorageLevel++;
        foreach (var s in Object.FindObjectsByType<CorralStorage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            s.NotifyMaxCapacityMayHaveChanged();
    }
}

