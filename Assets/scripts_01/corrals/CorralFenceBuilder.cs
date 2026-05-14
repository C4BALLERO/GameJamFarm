using UnityEngine;

/// <summary>
/// Genera una valla de “cubos” (cuadrados sólidos 2D) alrededor del área del corral.
/// Bloquea físicamente a los enemigos hasta que destruyen cada segmento.
/// Opcionalmente puede ocultar solo el dibujo de cada cubo para dejar ver cercas colocadas en la escena.
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralFenceBuilder : MonoBehaviour
{
    [SerializeField] private CorralZone zone;
    [Header("Generación")]
    [SerializeField] private bool buildAtStart = true;
    [Tooltip("Lado aproximado de cada cubo en unidades mundo.")]
    [SerializeField] private float cubeSize = 0.28f;
    [Tooltip("Separa la valla del borde interior del collider del corral hacia afuera.")]
    [SerializeField] private float outwardMargin = 0.14f;
    [SerializeField] private int hitPointsPerSegment = 5;
    [SerializeField] private Color fenceColor = new Color(0.42f, 0.3f, 0.2f, 1f);
    [SerializeField] private int sortingOrder = 4;
    [Header("Visual")]
    [Tooltip("Si está activo, desactiva solo el SpriteRenderer de cada FenceCube. Collider, CorralFenceSegment y daño siguen igual.")]
    [SerializeField] private bool hideProceduralFenceVisual = true;
    [Tooltip("Mínimo de cubos por lado (rectángulo pequeño).")]
    [SerializeField] [Min(2)] private int minSegmentsPerEdge = 2;

    private Transform _root;

    private void Awake()
    {
        if (zone == null)
            zone = GetComponent<CorralZone>();
    }

    private void Start()
    {
        if (buildAtStart)
            RebuildFence();
    }

    /// <summary>Regenera la valla (limpia hijos previos bajo CorralFenceRoot).</summary>
    public void RebuildFence()
    {
        if (zone == null)
            zone = GetComponent<CorralZone>();
        if (zone == null)
            return;

        var area = zone.AreaCollider;
        if (area == null)
            return;

        EnsureRoot();
        ClearChildren();

        var b = area.bounds;
        var min = b.min;
        var max = b.max;
        var z = transform.position.z;

        var step = Mathf.Max(0.18f, cubeSize);
        var half = step * 0.5f;
        var push = outwardMargin + half;

        PlaceEdgeHorizontal(min.x, max.x, min.y - push, z, step);
        PlaceEdgeHorizontal(min.x, max.x, max.y + push, z, step);
        PlaceEdgeVertical(min.x - push, min.y, max.y, z, step);
        PlaceEdgeVertical(max.x + push, min.y, max.y, z, step);
    }

    private void EnsureRoot()
    {
        if (_root != null)
            return;

        var existing = transform.Find("CorralFenceRoot");
        if (existing != null)
        {
            _root = existing;
            return;
        }

        var go = new GameObject("CorralFenceRoot");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.layer = gameObject.layer;
        _root = go.transform;
    }

    private void ClearChildren()
    {
        if (_root == null)
            return;
        for (var i = _root.childCount - 1; i >= 0; i--)
        {
            var ch = _root.GetChild(i);
            if (ch != null)
                Destroy(ch.gameObject);
        }
    }

    private void PlaceEdgeHorizontal(float xMin, float xMax, float y, float z, float step)
    {
        var length = Mathf.Max(0.01f, xMax - xMin);
        var count = Mathf.Max(minSegmentsPerEdge, Mathf.RoundToInt(length / step));
        var actual = length / count;

        for (var i = 0; i < count; i++)
        {
            var t = (i + 0.5f) / count;
            var x = Mathf.Lerp(xMin, xMax, t);
            CreateCubeAt(new Vector3(x, y, z), new Vector2(actual * 0.98f, step * 0.95f));
        }
    }

    private void PlaceEdgeVertical(float x, float yMin, float yMax, float z, float step)
    {
        var length = Mathf.Max(0.01f, yMax - yMin);
        var count = Mathf.Max(minSegmentsPerEdge, Mathf.RoundToInt(length / step));
        var actual = length / count;

        for (var i = 0; i < count; i++)
        {
            var t = (i + 0.5f) / count;
            var y = Mathf.Lerp(yMin, yMax, t);
            CreateCubeAt(new Vector3(x, y, z), new Vector2(step * 0.95f, actual * 0.98f));
        }
    }

    private void CreateCubeAt(Vector3 worldPos, Vector2 worldSize)
    {
        var go = new GameObject("FenceCube");
        go.transform.SetParent(_root, false);
        go.transform.position = worldPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(Mathf.Max(0.05f, worldSize.x), Mathf.Max(0.05f, worldSize.y), 1f);
        go.layer = gameObject.layer;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;

        go.AddComponent<BoxCollider2D>();

        var sr = go.AddComponent<SpriteRenderer>();
        var seg = go.AddComponent<CorralFenceSegment>();
        seg.Configure(hitPointsPerSegment, fenceColor, sortingOrder);
        if (hideProceduralFenceVisual)
            sr.enabled = false;
        go.AddComponent<FenceDamageVisual>();
    }
}
