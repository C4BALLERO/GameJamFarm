using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida del Granero en el HUD. Se auto-enlaza a BarnHealth en Start.
/// </summary>
[DisallowMultipleComponent]
public sealed class BarnHealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private BarnHealth _barn;

    private void Start()
    {
        var barn = FindFirstObjectByType<BarnHealth>();
        if (barn != null) Bind(barn);
    }

    public void Bind(BarnHealth barn)
    {
        if (slider == null) return;
        Unbind();
        _barn = barn;
        if (_barn == null) return;
        slider.maxValue = _barn.GetMaxHealth();
        slider.value    = _barn.CurrentHealth;
        _barn.HealthChanged += OnHealthChanged;
    }

    private void OnDestroy() => Unbind();

    private void Unbind()
    {
        if (_barn != null)
            _barn.HealthChanged -= OnHealthChanged;
        _barn = null;
    }

    private void OnHealthChanged(int current, int max)
    {
        if (slider == null) return;
        slider.maxValue = max;
        slider.value    = current;
    }
}
