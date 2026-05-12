using UnityEngine;

/// <summary>
/// Trigger hijo: recibe overlap de enemigos; expone <see cref="IDamageable"/> hacia el <see cref="CorralHealth"/> raíz.
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralDamageRelay : MonoBehaviour, IDamageable
{
    private CorralHealth _health;

    private void Awake()
    {
        _health = GetComponentInParent<CorralHealth>();
    }

    public void TakeDamage(int amount, Vector2 knockback)
    {
        _health?.TakeDamage(amount, knockback);
    }

    public int GetHealth() => _health != null ? _health.GetHealth() : 0;

    public int GetMaxHealth() => _health != null ? _health.GetMaxHealth() : 0;

    public bool IsDead => _health == null || _health.IsDead;

    public Transform Transform => _health != null ? _health.transform : transform;
}
