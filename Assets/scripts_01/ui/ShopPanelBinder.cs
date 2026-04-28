using UnityEngine;
using UnityEngine.UI;

/// <summary>Botones del panel Granero (escenas antiguas usan los mismos nombres de hijos).</summary>
[DisallowMultipleComponent]
public sealed class ShopPanelBinder : MonoBehaviour
{
    [SerializeField] private UIManager ui;

    private void Awake()
    {
        if (ui == null)
            ui = FindFirstObjectByType<UIManager>();

        if (ui == null) return;

        WireNamed("BuyCow", () => ui.BuyCow());
        WireNamed("BuyChicken", () => ui.BuyChicken());
        WireNamed("BuyPig", () => ui.BuyPig());

        WireNamed("SellCow", () => ui.BuyAttackUpgrade());
        WireNamed("SellChicken", () => ui.BuySpeedUpgrade());
        WireNamed("SellPig", () => ui.CloseShop());

        HideNamed("SellMilk");
        HideNamed("SellEgg");
        HideNamed("SellMeat");
    }

    private void WireNamed(string childName, UnityEngine.Events.UnityAction action)
    {
        var child = transform.Find(childName);
        if (child == null) return;
        var btn = child.GetComponent<Button>();
        Wire(btn, action);
    }

    private void HideNamed(string childName)
    {
        var child = transform.Find(childName);
        if (child != null)
            child.gameObject.SetActive(false);
    }

    private static void Wire(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null || action == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(action);
    }
}
