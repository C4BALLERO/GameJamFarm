using UnityEngine;
using UnityEngine.UI;

public sealed class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    public void Bind(PlayerHealth health)
    {
        if (health == null || slider == null) return;
        slider.maxValue = health.MaxHealth;
        slider.value = health.CurrentHealth;
        health.HealthChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        // MVP: unbind handled by UIManager if desired.
    }

    private void OnHealthChanged(int current, int max)
    {
        if (slider == null) return;
        slider.maxValue = max;
        slider.value = current;
    }
}

