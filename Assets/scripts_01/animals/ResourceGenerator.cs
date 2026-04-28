using UnityEngine;

[DisallowMultipleComponent]
public sealed class ResourceGenerator : MonoBehaviour
{
    [SerializeField] private ResourceType resourceType = ResourceType.Meat;
    [SerializeField] private int amountPerTick = 1;
    [SerializeField] private float secondsPerTick = 6f;
    [SerializeField] private InventorySystem inventory;
    [SerializeField] private AnimalBase animal;

    private float _nextTickAt;

    public void Configure(ResourceType type, int amount, float seconds)
    {
        resourceType = type;
        amountPerTick = Mathf.Max(1, amount);
        secondsPerTick = Mathf.Max(0.25f, seconds);
        _nextTickAt = Time.time + secondsPerTick;
    }

    public void Init(InventorySystem inv)
    {
        inventory = inv;
        _nextTickAt = Time.time + secondsPerTick;
    }

    private void Reset()
    {
        animal = GetComponent<AnimalBase>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen) return;
        if (animal != null && animal.IsDead) return;
        if (inventory == null) return;
        if (Time.time < _nextTickAt) return;

        _nextTickAt = Time.time + Mathf.Max(0.25f, secondsPerTick);
        inventory.Add(resourceType, Mathf.Max(1, amountPerTick));
    }
}

