using UnityEngine;

/// <summary>
/// Enumeration of all resource types available in the game.
/// </summary>
public enum ResourceType
{
    Wood = 0,
    Stone = 1,
    Food = 2,
    Gold = 3,
    Crops = 4
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

