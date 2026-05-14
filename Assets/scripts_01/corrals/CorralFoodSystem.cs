using UnityEngine;

/// <summary>
/// Depósito inventario → corral con capacidad estricta: no se pierde comida del inventario ni se supera el máximo.
/// </summary>
public static class CorralFoodSystem
{
    /// <summary>Deposita pienso básico 1:1 hasta <paramref name="maxWithdraw"/> unidades.</summary>
    public static bool TryDepositBasic(
        CorralFoodStorage food,
        InventorySystem inv,
        int maxWithdraw,
        out int withdrawnFromInventory,
        out int addedToCorral)
    {
        withdrawnFromInventory = 0;
        addedToCorral = 0;
        if (food == null || inv == null || maxWithdraw <= 0)
            return false;

        var room = CorralCapacityValidator.GetRemainingFoodSlots(food);
        if (room <= 0)
            return false;

        var have = inv.Get(ResourceType.FeedBasic);
        if (have <= 0)
            return false;

        var take = Mathf.Min(maxWithdraw, have, room);
        if (take <= 0)
            return false;

        if (!inv.Remove(ResourceType.FeedBasic, take))
            return false;

        var added = food.TryAddFood(take);
        if (added != take)
        {
            inv.Add(ResourceType.FeedBasic, take);
            return false;
        }

        withdrawnFromInventory = added;
        addedToCorral = added;
        return true;
    }

    /// <summary>Cada unidad de premium en inventario aporta <paramref name="foodValuePerUnit"/> de comida al corral.</summary>
    public static bool TryDepositPremium(
        CorralFoodStorage food,
        InventorySystem inv,
        int maxWithdrawPackages,
        int foodValuePerUnit,
        out int withdrawnPackages,
        out int addedToCorral)
    {
        withdrawnPackages = 0;
        addedToCorral = 0;
        if (food == null || inv == null || maxWithdrawPackages <= 0 || foodValuePerUnit <= 0)
            return false;

        var room = CorralCapacityValidator.GetRemainingFoodSlots(food);
        if (room <= 0)
            return false;

        var have = inv.Get(ResourceType.FeedPremium);
        if (have <= 0)
            return false;

        var maxPackagesByRoom = room / foodValuePerUnit;
        var packages = Mathf.Min(maxWithdrawPackages, have, maxPackagesByRoom);
        if (packages <= 0)
            return false;

        var foodToAdd = packages * foodValuePerUnit;

        if (!inv.Remove(ResourceType.FeedPremium, packages))
            return false;

        var added = food.TryAddFood(foodToAdd);
        if (added != foodToAdd)
        {
            inv.Add(ResourceType.FeedPremium, packages);
            return false;
        }

        withdrawnPackages = packages;
        addedToCorral = added;
        return true;
    }
}
