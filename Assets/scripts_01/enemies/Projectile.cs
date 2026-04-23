using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float lifeSeconds = 3f;

    private LayerMask _hitLayers;
    private int _damage;
    private float _knockback;
    private float _dieAt;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        _dieAt = Time.time + Mathf.Max(0.1f, lifeSeconds);
    }

    private void Update()
    {
        if (Time.time >= _dieAt) Destroy(gameObject);
    }

    public void Fire(Vector2 velocity, int damage, float knockback, LayerMask hitLayers)
    {
        _damage = damage;
        _knockback = knockback;
        _hitLayers = hitLayers;
        rb.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & _hitLayers.value) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var dmg) && !dmg.IsDead)
        {
            var dir = ((Vector2)dmg.Transform.position - (Vector2)transform.position);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();
            dmg.TakeDamage(_damage, dir * _knockback);
        }

        Destroy(gameObject);
    }
}

