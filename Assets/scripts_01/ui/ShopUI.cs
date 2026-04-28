using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Panel tienda Granero: pausa el juego al abrirse.</summary>
[DisallowMultipleComponent]
public sealed class ShopUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject panelRoot;

    [Header("Refs")]
    [SerializeField] private ShopSystem shopSystem;
    [SerializeField] private InventorySystem inventory;

    [Header("Etiquetas de coste (opcional)")]
    [SerializeField] private Text cowCostText;
    [SerializeField] private Text chickenCostText;
    [SerializeField] private Text pigCostText;
    [SerializeField] private Text attackCostText;
    [SerializeField] private Text speedCostText;

    private readonly StringBuilder _sb = new();

    private void Awake()
    {
        if (shopSystem == null)
            shopSystem = FindFirstObjectByType<ShopSystem>();
        if (inventory == null)
            inventory = FindFirstObjectByType<InventorySystem>();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>Opción para escenas montadas a mano (sin ShopUIService del builder).</summary>
    public void BindScene(GameObject root, ShopSystem shop, InventorySystem inv)
    {
        panelRoot = root;
        shopSystem = shop;
        inventory = inv;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.ResourceChanged += OnInventoryChanged;

        RefreshLabels();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.ResourceChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged(ResourceType arg1, int arg2) => RefreshLabels();

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshLabels();

        if (GameManager.Instance != null)
            GameManager.Instance.SetShopPaused(true);
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetShopPaused(false);
    }

    public void Toggle()
    {
        if (panelRoot == null)
            return;

        var next = !panelRoot.activeSelf;
        panelRoot.SetActive(next);

        if (GameManager.Instance != null)
            GameManager.Instance.SetShopPaused(next);

        if (next)
            RefreshLabels();
    }

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void RefreshLabels()
    {
        if (shopSystem == null)
            shopSystem = FindFirstObjectByType<ShopSystem>();

        if (shopSystem == null)
            return;

        FormatLine(cowCostText, shopSystem.GetPurchaseCosts(FarmAnimalKind.Cow));
        FormatLine(chickenCostText, shopSystem.GetPurchaseCosts(FarmAnimalKind.Chicken));
        FormatLine(pigCostText, shopSystem.GetPurchaseCosts(FarmAnimalKind.Pig));
        FormatLine(attackCostText, shopSystem.GetAttackUpgradeCosts());
        FormatLine(speedCostText, shopSystem.GetSpeedUpgradeCosts());
    }

    private void FormatLine(Text label, ResourceCost[] costs)
    {
        if (label == null) return;

        _sb.Clear();
        if (costs == null || costs.Length == 0)
        {
            label.text = "—";
            return;
        }

        for (var i = 0; i < costs.Length; i++)
        {
            var c = costs[i];
            if (c.amount <= 0) continue;
            if (_sb.Length > 0) _sb.Append(", ");
            _sb.Append(c.amount);
            _sb.Append(' ');
            _sb.Append(ResourceLabel(c.type));
        }

        label.text = _sb.Length > 0 ? _sb.ToString() : "—";
    }

    private static string ResourceLabel(ResourceType t)
    {
        return t switch
        {
            ResourceType.Milk => "Leche",
            ResourceType.Egg => "Huevos",
            ResourceType.Meat => "Carne",
            _ => t.ToString()
        };
    }
}
