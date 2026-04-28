using UnityEngine;

public enum FarmAnimalKind
{
    Cow = 0,
    Chicken = 1,
    Pig = 2
}

/// <summary>
/// Concrete farm animal used for shop-spawned livestock. Attach <see cref="ResourceGenerator"/> on the same object.
/// </summary>
[DisallowMultipleComponent]
public sealed class FarmAnimal : AnimalBase
{
    [SerializeField] private FarmAnimalKind kind = FarmAnimalKind.Cow;
    [SerializeField] private int sellGoldValue = 18;
    [SerializeField] private int amountPerTick = 1;
    [SerializeField] private float secondsPerTick = 6f;

    public FarmAnimalKind Kind => kind;
    public int SellGoldValue => Mathf.Max(1, sellGoldValue);

    protected override void Awake()
    {
        base.Awake();

        if (TryGetComponent<ResourceGenerator>(out var generator))
        {
            var type = kind switch
            {
                FarmAnimalKind.Cow => ResourceType.Milk,
                FarmAnimalKind.Chicken => ResourceType.Egg,
                _ => ResourceType.Meat
            };
            generator.Configure(type, amountPerTick, secondsPerTick);
        }
    }
}
