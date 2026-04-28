using UnityEngine;

/// <summary>Únicos recursos del juego: producción animal → moneda de la tienda (Granero).</summary>
public enum ResourceType
{
    Milk = 0,
    Egg = 1,
    Meat = 2
}

[System.Serializable]
public class ResourceData
{
    public ResourceType type;
    public int amount;
    public string displayName;
    public Sprite icon;
}
