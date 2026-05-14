using UnityEngine;

/// <summary>
/// Validación centralizada de capacidad de comida en corrales (sin sobrecapacidad nunca).
/// </summary>
public static class CorralCapacityValidator
{
    public const string CorralFoodFullMessage = "Corral Food Full";

    public static int GetRemainingFoodSlots(CorralFoodStorage storage)
    {
        if (storage == null)
            return 0;
        return Mathf.Max(0, storage.MaxFood - storage.CurrentFood);
    }

    public static bool HasRoomForFood(CorralFoodStorage storage, int atLeast = 1)
    {
        return GetRemainingFoodSlots(storage) >= Mathf.Max(1, atLeast);
    }
}
