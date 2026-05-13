using UnityEngine;

/// <summary>
/// Precios de venta y balance. Huevos: 2 = 1 moneda (solo pares al vender todo). Leche: 3/u. Carne: 2/u.
/// </summary>
[DisallowMultipleComponent]
public sealed class EconomySystem : MonoBehaviour
{
    public static EconomySystem Instance { get; private set; }

    [Header("Icono moneda (HUD / tienda)")]
    [SerializeField] private Sprite coinSprite;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureCoinSpriteLoaded();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Monedas obtenidas al vender <paramref name="amount"/> unidades (reglas actuales × multiplicador granero).</summary>
    public int ComputeSellCoins(ResourceType type, int amount, float barnSellRewardMultiplier = 1f)
    {
        if (amount <= 0)
            return 0;
        var m = Mathf.Max(0f, barnSellRewardMultiplier);
        return type switch
        {
            ResourceType.Egg => Mathf.RoundToInt((amount / 2) * 1f * m),
            ResourceType.Milk => Mathf.RoundToInt(amount * 3f * m),
            ResourceType.Meat => Mathf.RoundToInt(amount * 2f * m),
            _ => 0
        };
    }

    /// <summary>Cuántas unidades se retiran del inventario al pulsar «vender todo» (huevos impares dejan 1).</summary>
    public int CountResourcesConsumedOnSellAll(ResourceType type, int amount)
    {
        if (amount <= 0)
            return 0;
        if (type == ResourceType.Egg)
            return (amount / 2) * 2;
        return amount;
    }

    public Sprite CoinSprite =>
        coinSprite != null
            ? coinSprite
            : Resources.Load<Sprite>("MonedaIcono") ?? Resources.Load<Sprite>("Coin");

    private void EnsureCoinSpriteLoaded()
    {
        if (coinSprite != null)
            return;

        foreach (var key in new[] { "MonedaIcono", "monedaIcono", "Coin", "coin", "Moneda", "moneda" })
        {
            coinSprite = Resources.Load<Sprite>(key);
            if (coinSprite != null)
                return;
        }
    }
}
