using UnityEngine;

/// <summary>
/// Precios de venta y referencia de balance (editar en Inspector en <c>GameSystems</c>).
/// </summary>
[DisallowMultipleComponent]
public sealed class EconomySystem : MonoBehaviour
{
    public static EconomySystem Instance { get; private set; }

    [Header("Venta: monedas por unidad de recurso")]
    [Tooltip("Leche: producción más lenta, mayor valor.")]
    [SerializeField] private int sellCoinsPerMilk = 4;
    [Tooltip("Huevos: producción rápida, menor valor.")]
    [SerializeField] private int sellCoinsPerEgg = 1;
    [SerializeField] private int sellCoinsPerMeat = 3;

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

    public int GetSellCoinsPerUnit(ResourceType type)
    {
        return type switch
        {
            ResourceType.Milk => Mathf.Max(0, sellCoinsPerMilk),
            ResourceType.Egg => Mathf.Max(0, sellCoinsPerEgg),
            ResourceType.Meat => Mathf.Max(0, sellCoinsPerMeat),
            _ => 0
        };
    }

    public Sprite CoinSprite => coinSprite != null ? coinSprite : Resources.Load<Sprite>("Coin");

    /// <summary>Intenta cargar icono desde <c>Resources</c> si no está asignado en Inspector.</summary>
    private void EnsureCoinSpriteLoaded()
    {
        if (coinSprite != null)
            return;

        foreach (var key in new[] { "Coin", "coin", "Moneda", "moneda" })
        {
            coinSprite = Resources.Load<Sprite>(key);
            if (coinSprite != null)
                return;
        }
    }
}
