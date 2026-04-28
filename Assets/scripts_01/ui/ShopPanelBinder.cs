using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires shop / sell <see cref="Button"/> clicks to <see cref="UIManager"/> at runtime (build-friendly, no persistent UnityEvents required).
/// </summary>
[DisallowMultipleComponent]
public sealed class ShopPanelBinder : MonoBehaviour
{
    [SerializeField] private UIManager ui;
    [SerializeField] private Button buyCow;
    [SerializeField] private Button buyChicken;
    [SerializeField] private Button buyPig;
    [SerializeField] private Button sellMilk;
    [SerializeField] private Button sellEgg;
    [SerializeField] private Button sellMeat;
    [SerializeField] private Button sellCow;
    [SerializeField] private Button sellChicken;
    [SerializeField] private Button sellPig;

    private void Awake()
    {
        if (ui == null)
            ui = FindFirstObjectByType<UIManager>();

        if (ui == null) return;

        Wire(buyCow, () => ui.BuyCow());
        Wire(buyChicken, () => ui.BuyChicken());
        Wire(buyPig, () => ui.BuyPig());
        Wire(sellMilk, () => ui.SellMilkBatch());
        Wire(sellEgg, () => ui.SellEggBatch());
        Wire(sellMeat, () => ui.SellMeatBatch());
        Wire(sellCow, () => ui.SellCow());
        Wire(sellChicken, () => ui.SellChicken());
        Wire(sellPig, () => ui.SellPig());
    }

    private static void Wire(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null || action == null) return;
        b.onClick.AddListener(action);
    }
}
