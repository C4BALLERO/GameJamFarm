using UnityEngine;

/// <summary>
/// One-time world tuning for this project (top-down: no gravity on rigidbodies).
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldSettings : MonoBehaviour
{
    [SerializeField] private Vector2 gravity2D = Vector2.zero;

    private void Awake()
    {
        Physics2D.gravity = gravity2D;
    }
}
