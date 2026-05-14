using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float hitboxActiveSeconds = 0.12f;
    [SerializeField] private int damage = 2;
    [SerializeField] private float knockbackForce = 3.5f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Hitbox")]
    [SerializeField] private Collider2D hitboxCollider;
    [SerializeField] private float hitboxDistance = 0.45f;

    [Header("Mejoras (granero)")]
    [SerializeField] private float cooldownReductionPerTier = 0.055f;
    [SerializeField] private int damageBonusPerTier = 1;

    private float _nextAttackAt;
    private float _disableAt;
    private int _attackTier;

    public int AttackTier => _attackTier;

    private void Reset()
    {
        hitboxCollider = GetComponentInChildren<Collider2D>();
    }

    private void Awake()
    {
        if (hitboxCollider != null) hitboxCollider.enabled = false;
    }

    private void Update()
    {
        if (_disableAt > 0f && Time.time >= _disableAt)
        {
            _disableAt = 0f;
            if (hitboxCollider != null) hitboxCollider.enabled = false;
        }
    }

    /// <summary>Sube fuerza de ataque y velocidad de golpes (compra en tienda).</summary>
    public void IncrementAttackTier()
    {
        _attackTier++;
    }

    private float EffectiveCooldown =>
        Mathf.Max(0.08f, attackCooldown * (1f - cooldownReductionPerTier * Mathf.Min(_attackTier, 12)));

    private int EffectiveDamage =>
        Mathf.Max(1, damage + damageBonusPerTier * _attackTier);

    public bool TryAttack(Vector2 facing)
    {
        if (Time.time < _nextAttackAt) return false;
        _nextAttackAt = Time.time + EffectiveCooldown;

        if (hitboxCollider == null) return true;

        var dir = facing.sqrMagnitude > 0.001f ? facing.normalized : Vector2.down;
        hitboxCollider.transform.position = (Vector2)transform.position + dir * hitboxDistance;

        hitboxCollider.enabled = true;
        _disableAt = Time.time + Mathf.Max(0.02f, hitboxActiveSeconds);
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hitboxCollider == null || !hitboxCollider.enabled) return;
        if (((1 << other.gameObject.layer) & enemyLayers.value) == 0) return;
        if (!DamageableResolver.TryResolve(other, out var dmg)) return;
        if (dmg.IsDead) return;

        var dir = ((Vector2)dmg.Transform.position - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();
        dmg.TakeDamage(EffectiveDamage, dir * knockbackForce);
    }
}
