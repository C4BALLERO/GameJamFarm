using UnityEngine;

/// <summary>
/// Lightweight 2D camera follow used when Cinemachine is not configured.
/// Keeps a wider orthographic view for better awareness.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
    [SerializeField] private float followSmooth = 8f;
    [SerializeField] private float orthographicSize = 8.5f;

    private Camera _cam;

    public void SetTarget(Transform t) => target = t;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam != null && _cam.orthographic)
            _cam.orthographicSize = Mathf.Max(3f, orthographicSize);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        var desired = target.position + offset;
        desired.z = offset.z;

        var t = 1f - Mathf.Exp(-Mathf.Max(0.01f, followSmooth) * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);

        if (_cam != null && _cam.orthographic)
            _cam.orthographicSize = Mathf.Max(3f, orthographicSize);
    }
}
