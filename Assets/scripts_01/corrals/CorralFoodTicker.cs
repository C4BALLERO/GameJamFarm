using UnityEngine;

/// <summary>
/// Consumo periódico de comida por animales en cada corral. Un solo ticker en <see cref="CorralManager"/> evita N Updates.
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralFoodTicker : MonoBehaviour
{
    [SerializeField] private CorralManager corrals;
    [Tooltip("Segundos entre ticks de hambre en todos los corrales.")]
    [SerializeField] private float tickIntervalSeconds = 6f;
    [Tooltip("Comida consumida por tick y por animal (vacas).")]
    [SerializeField] private int cowFoodPerAnimalPerTick = 2;
    [SerializeField] private int chickenFoodPerAnimalPerTick = 1;
    [SerializeField] private int pigFoodPerAnimalPerTick = 3;

    private float _nextTick;

    private void Awake()
    {
        if (corrals == null)
            corrals = FindFirstObjectByType<CorralManager>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen)
            return;
        if (corrals == null)
            return;
        if (Time.time < _nextTick)
            return;
        _nextTick = Time.time + Mathf.Max(1f, tickIntervalSeconds);

        TickCorral(FarmAnimalKind.Cow, cowFoodPerAnimalPerTick);
        TickCorral(FarmAnimalKind.Chicken, chickenFoodPerAnimalPerTick);
        TickCorral(FarmAnimalKind.Pig, pigFoodPerAnimalPerTick);
    }

    private void TickCorral(FarmAnimalKind kind, int perAnimal)
    {
        var zone = corrals.GetZone(kind);
        if (zone == null)
            return;
        var n = zone.CurrentCount;
        if (n <= 0)
            return;
        var food = zone.GetComponent<CorralFoodStorage>();
        if (food == null)
            return;
        var demand = Mathf.Max(0, perAnimal) * n;
        food.ConsumeUpTo(demand);
    }
}
