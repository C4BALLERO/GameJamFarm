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

    private float _nextAttackAt;
    private float _disableAt;

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

    public bool TryAttack(Vector2 facing)
    {
        if (Time.time < _nextAttackAt) return false;
        _nextAttackAt = Time.time + Mathf.Max(0.05f, attackCooldown);

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
        if (!other.TryGetComponent<IDamageable>(out var dmg)) return;
        if (dmg.IsDead) return;

        var dir = ((Vector2)dmg.Transform.position - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();
        dmg.TakeDamage(damage, dir * knockbackForce);
    }
}

