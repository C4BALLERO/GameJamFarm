using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One fenced area: only one <see cref="FarmAnimalKind"/>, capacity, and spawn sampling.
/// Bounds: prefer a child named zonaVacas / zonaPollos / zonaCerdos (o sonaVacas…) con Collider2D;
/// si no hay, usa el collider del mismo objeto.
/// </summary>
[DisallowMultipleComponent]
public sealed class CorralZone : MonoBehaviour
{
    [Header("Rules")]
    [SerializeField] private FarmAnimalKind allowedKind = FarmAnimalKind.Cow;
    [SerializeField] [Min(1)] private int maxAnimals = 8;

    [Header("Bounds")]
    [SerializeField] private Collider2D area;

    [Header("Spawn points (optional)")]
    [Tooltip("If empty, children named SpawnPoint are used. If still empty, positions are random inside Collider2D bounds.")]
    [SerializeField] private Transform[] spawnPointOverrides;

    private readonly List<GameObject> _occupants = new();

    public FarmAnimalKind AllowedKind => allowedKind;

    public int CurrentCount => _occupants.Count;

    public int MaxAnimals => maxAnimals;

    public bool HasCapacity() => _occupants.Count < maxAnimals;

    public Collider2D AreaCollider => area;

    private void Reset()
    {
        area = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        ResolveInteriorArea();
    }

    private void Start()
    {
        ResolveInteriorArea();
    }

    /// <summary>Escenas montadas a mano: asignar especie tras crear el componente desde código.</summary>
    public void ApplyKind(FarmAnimalKind kind)
    {
        allowedKind = kind;
        ResolveInteriorArea();
    }

    /// <summary>Si se añadió un collider después del Awake inicial.</summary>
    public void RefreshAreaReference()
    {
        ResolveInteriorArea();
    }

    /// <summary>
    /// Prioriza un hijo “zona…” / “sona…” con Collider2D (interior útil para tilemaps); si no, collider en la raíz.
    /// </summary>
    private void ResolveInteriorArea()
    {
        var interior = FindPreferredInteriorCollider();
        if (interior != null)
        {
            area = interior;
            return;
        }

        if (area == null)
            area = GetComponent<Collider2D>();
    }

    private Collider2D FindPreferredInteriorCollider()
    {
        foreach (var nm in InteriorNamesForKind(allowedKind))
        {
            var t = transform.Find(nm);
            if (t != null && t.TryGetComponent<Collider2D>(out var c))
                return c;
        }

        for (var i = 0; i < transform.childCount; i++)
        {
            var ch = transform.GetChild(i);
            var nl = ch.name.ToLowerInvariant();
            if ((nl.Contains("zona") || nl.Contains("sona")) &&
                ch.TryGetComponent<Collider2D>(out var c2))
                return c2;
        }

        return null;
    }

    private static string[] InteriorNamesForKind(FarmAnimalKind kind)
    {
        return kind switch
        {
            FarmAnimalKind.Cow => new[] { "zonaVacas", "sonaVacas", "ZonaVacas", "SonaVacas" },
            FarmAnimalKind.Chicken => new[] { "zonaPollos", "sonaPollos", "ZonaPollos", "SonaPollos" },
            FarmAnimalKind.Pig => new[] { "zonaCerdos", "sonaCerdos", "ZonaCerdos", "SonaCerdos" },
            _ => Array.Empty<string>()
        };
    }

    private void OnValidate()
    {
        maxAnimals = Mathf.Max(1, maxAnimals);
#if UNITY_EDITOR
        ResolveInteriorArea();
#endif
    }

    /// <summary>Returns random position inside corral (spawn point jitter or bounds).</summary>
    public Vector3 GetRandomSpawnPosition()
    {
        var pts = GatherSpawnPoints();
        if (pts.Count > 0)
        {
            var t = pts[UnityEngine.Random.Range(0, pts.Count)];
            var jitter = (Vector3)(UnityEngine.Random.insideUnitCircle * 0.35f);
            return t.position + jitter;
        }

        if (area != null)
        {
            var b = area.bounds;
            var z = transform.position.z;
            return new Vector3(UnityEngine.Random.Range(b.min.x, b.max.x), UnityEngine.Random.Range(b.min.y, b.max.y), z);
        }

        return transform.position;
    }

    /// <summary>Used by enemy spawn exclusion.</summary>
    public bool ContainsPoint(Vector2 worldPoint)
    {
        if (area == null) return false;
        return area.OverlapPoint(worldPoint);
    }

    /// <summary>Keeps a rigidbody inside this zone each physics step.</summary>
    public bool ClampInside(ref Vector2 position)
    {
        if (area == null) return false;
        var b = area.bounds;
        var ox = position.x < b.min.x || position.x > b.max.x;
        var oy = position.y < b.min.y || position.y > b.max.y;
        if (!ox && !oy) return false;

        position.x = Mathf.Clamp(position.x, b.min.x, b.max.x);
        position.y = Mathf.Clamp(position.y, b.min.y, b.max.y);
        return true;
    }

    /// <summary>Clamp a world position to the collider bounds with an inward margin (keeps sprites inside the fence).</summary>
    public Vector2 ClampPointToArea(Vector2 worldPoint, float inset)
    {
        if (area == null) return worldPoint;
        var b = area.bounds;
        var ix = Mathf.Max(0f, inset);
        var hx = b.size.x * 0.5f;
        var hy = b.size.y * 0.5f;
        if (ix >= hx || ix >= hy) ix = 0f;
        return new Vector2(
            Mathf.Clamp(worldPoint.x, b.min.x + ix, b.max.x - ix),
            Mathf.Clamp(worldPoint.y, b.min.y + ix, b.max.y - ix));
    }

    public void RegisterOccupant(GameObject instance)
    {
        if (instance == null) return;
        if (_occupants.Contains(instance)) return;
        _occupants.Add(instance);
    }

    public void UnregisterOccupant(GameObject instance)
    {
        if (instance == null) return;
        _occupants.Remove(instance);
    }

    private List<Transform> GatherSpawnPoints()
    {
        var list = new List<Transform>();

        if (spawnPointOverrides != null && spawnPointOverrides.Length > 0)
        {
            foreach (var t in spawnPointOverrides)
                if (t != null) list.Add(t);
            return list;
        }

        GatherSpawnPointsRecursive(transform, list);
        return list;
    }

    private static void GatherSpawnPointsRecursive(Transform t, List<Transform> list)
    {
        for (var i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            if (c.name.IndexOf("SpawnPoint", StringComparison.OrdinalIgnoreCase) >= 0)
                list.Add(c);
            GatherSpawnPointsRecursive(c, list);
        }
    }
}
