using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a UI <see cref="Slider"/> to <see cref="PlayerHealth"/> and keeps it in sync.
/// </summary>
public sealed class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private PlayerHealth _bound;

    public void Bind(PlayerHealth health)
    {
        if (slider == null) return;
        Unbind();
        _bound = health;
        if (_bound == null) return;

        slider.maxValue = _bound.MaxHealth;
        slider.value = _bound.CurrentHealth;
        _bound.HealthChanged += OnHealthChanged;
    }

    private void OnDestroy() => Unbind();

    private void Unbind()
    {
        if (_bound != null)
            _bound.HealthChanged -= OnHealthChanged;
        _bound = null;
    }

    private void OnHealthChanged(int current, int max)
    {
        if (slider == null) return;
        slider.maxValue = max;
        slider.value = current;
    }
}


