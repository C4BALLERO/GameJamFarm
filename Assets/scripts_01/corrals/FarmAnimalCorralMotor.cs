using UnityEngine;

/// <summary>
/// Wander inside the corral using <see cref="Rigidbody2D.MovePosition"/> (works with kinematic bodies).
/// Exposes <see cref="LastVelocity"/> so <see cref="AnimalSpriteAnimator"/> can play walk vs idle correctly.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class FarmAnimalCorralMotor : MonoBehaviour
{
    [SerializeField] private float wanderSpeed = 0.55f;
    [SerializeField] private float directionChangeSeconds = 2.4f;
    [SerializeField] private float separationRadius = 0.55f;
    [SerializeField] private float separationForce = 2.5f;
    [SerializeField] private float edgeInset = 0.38f;

    private CorralZone _zone;
    private Rigidbody2D _rb;
    private AnimalBase _animal;
    private Vector2 _dir;
    private float _nextDirAt;

    /// <summary>Last intended physics velocity (used for walk animation when RB is kinematic).</summary>
    public Vector2 LastVelocity { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        TryGetComponent(out _animal);
    }

    /// <summary>Called when instantiated into a corral.</summary>
    public void Attach(CorralZone zone)
    {
        _zone = zone;
        PickNewDirection(true);
    }

    private void OnDestroy()
    {
        if (_zone != null)
            _zone.UnregisterOccupant(gameObject);
    }

    private void FixedUpdate()
    {
        if (_animal != null && _animal.IsDead)
        {
            LastVelocity = Vector2.zero;
            return;
        }

        if (_zone == null || _rb == null)
        {
            LastVelocity = Vector2.zero;
            return;
        }

        if (Time.time >= _nextDirAt)
            PickNewDirection(false);

        var vel = _dir * wanderSpeed;
        ApplySeparation(ref vel);
        LastVelocity = vel;

        var dt = Time.fixedDeltaTime;
        var next = _rb.position + vel * dt;
        next = _zone.ClampPointToArea(next, edgeInset);
        _rb.MovePosition(next);
    }

    private void PickNewDirection(bool immediate)
    {
        _dir = Random.insideUnitCircle;
        if (_dir.sqrMagnitude < 0.001f) _dir = Vector2.up;
        _dir.Normalize();
        var mul = immediate ? 0.6f : 1f;
        _nextDirAt = Time.time + directionChangeSeconds * Random.Range(0.65f, 1.35f) * mul;
    }

    private void ApplySeparation(ref Vector2 velocity)
    {
        if (separationRadius <= 0f || separationForce <= 0f)
            return;

        var hits = Physics2D.OverlapCircleAll(_rb.position, separationRadius);
        var push = Vector2.zero;
        var count = 0;

        foreach (var h in hits)
        {
            if (h.attachedRigidbody == null || h.attachedRigidbody == _rb) continue;
            if (!h.attachedRigidbody.TryGetComponent<FarmAnimalCorralMotor>(out _))
                continue;

            var delta = _rb.position - h.attachedRigidbody.position;
            var d = delta.magnitude;
            if (d < 0.05f) continue;
            push += delta.normalized * (separationRadius - d);
            count++;
        }

        if (count > 0)
            velocity += push * (separationForce / count);
    }
}
