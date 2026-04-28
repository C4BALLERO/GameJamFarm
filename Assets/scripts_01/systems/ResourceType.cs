using UnityEngine;

/// <summary>
/// Enumeration of all resource types available in the game.
/// </summary>
public enum ResourceType
{
    Wood = 0,
    Stone = 1,
    Milk = 2,
    Gold = 3,
    Egg = 4,
    Meat = 5
}

/// <summary>
/// Data holder for resource information with configuration in scriptable objects.
/// </summary>
[System.Serializable]
public class ResourceData
{
    public ResourceType type;
    public int amount;
    public string displayName;
    public Sprite icon;
}

