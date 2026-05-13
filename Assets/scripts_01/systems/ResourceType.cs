using UnityEngine;

/// <summary>Únicos recursos del juego: producción animal → moneda de la tienda (Granero).</summary>
public enum ResourceType
{
    Milk = 0,
    Egg = 1,
    Meat = 2,
    Coin = 3,
    /// <summary>Pienso básico (compra en granero, se deposita en corrales).</summary>
    FeedBasic = 4,
    /// <summary>Pienso enriquecido: menos unidades en inventario pero rinde más al depositar.</summary>
    FeedPremium = 5
}

[System.Serializable]
public class ResourceData
{
    public ResourceType type;
    public int amount;
    public string displayName;
    public Sprite icon;
}
