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

        WireNamed("BtnMejorarVida", () => ui.BuyAnimalHealthPowerUp());
        WireNamed("BtnMejorarProduccion", () => ui.BuyFasterGenerationPowerUp());
        WireNamed("BtnAtaqueAnimales", () => ui.BuyPlayerDamagePowerUp());
        WireNamed("BtnMejorarVelocidad", () => ui.BuySpeedUpgrade());
        WireNamed("BtnMejorarAtaque", () => ui.BuyAttackUpgrade());
        WireNamed("BtnCerrarTienda", () => ui.CloseShop());
    }

    private void WireNamed(string childName, UnityEngine.Events.UnityAction action)
    {
        var child = transform.Find(childName);
        if (child == null) return;
        var btn = child.GetComponent<Button>();
        Wire(btn, action);
    }

    private static void Wire(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null || action == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(action);
    }
}
