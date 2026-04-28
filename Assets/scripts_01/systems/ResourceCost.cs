using System;
using UnityEngine;

/// <summary>
/// Single line in a composite purchase price (inventory resources as currency).
/// </summary>
[Serializable]
public struct ResourceCost
{
    public ResourceType type;
    [Min(0)] public int amount;
}
