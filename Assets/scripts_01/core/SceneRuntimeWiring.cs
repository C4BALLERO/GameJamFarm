using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Finalizes references that are awkward to serialize across prefabs (inventory for animals, spawn player ref, UI bind).
/// Runs after other <see cref="MonoBehaviour.Start"/> calls by default execution order.
/// </summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class SceneRuntimeWiring : MonoBehaviour
{
    private void Awake()
    {
        FarmSceneGameplayBootstrap.Run();
    }

    private void Start()
    {
        var inv = FindFirstObjectByType<InventorySystem>();
        foreach (var gen in FindObjectsByType<ResourceGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (inv != null)
                gen.Init(inv);
        }

        var shop = FindFirstObjectByType<ShopSystem>();
        var powerUps = FindFirstObjectByType<PowerUpSystem>();
        if (shop != null && inv != null)
            shop.Bind(inv);
        if (shop != null && powerUps != null)
            shop.BindPowerUps(powerUps);

        var animalSpawner = FindFirstObjectByType<AnimalSpawner>();
        if (shop != null && animalSpawner != null)
            shop.BindSpawner(animalSpawner);

        var spawner = FindFirstObjectByType<SpawnManager>();
        var corrals = FindFirstObjectByType<CorralManager>();
        var player = FindFirstObjectByType<PlayerController>();
        if (spawner != null && player != null)
            spawner.SetPlayer(player.transform);
        if (spawner != null && corrals != null)
            spawner.SetCorralManager(corrals);
        if (player != null && player.TryGetComponent<PlayerHealth>(out var playerHealth))
            playerHealth.SetRespawnPoint(player.transform.position);

        if (player != null && Camera.main != null)
        {
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            if (follow == null)
                follow = Camera.main.gameObject.AddComponent<CameraFollow2D>();
            follow.SetTarget(player.transform);
        }

        var tm = FindFirstObjectByType<TimeManager>();
        var dayNight = FindFirstObjectByType<DayNightManager>();
        if (spawner != null && tm != null)
            spawner.SetTimeManager(tm);
        if (spawner != null && dayNight != null)
            spawner.SetDayNightManager(dayNight);

        var ui = FindFirstObjectByType<UIManager>();
        if (ui != null && inv != null && player != null && player.TryGetComponent<PlayerHealth>(out var ph))
            ui.Bind(ph, inv);
    }
}

/// <summary>
/// En escenas ya montadas (tilemaps, Granero colocado a mano): crea/enlaza ShopUI, collider del Granero y botones del panel.
/// Corre muy pronto desde <see cref="SceneRuntimeWiring"/> para que <see cref="BarnShopTrigger"/> encuentre la tienda.
/// </summary>
public static class FarmSceneGameplayBootstrap
{
    private static readonly (string ObjectName, FarmAnimalKind Kind)[] NamedCorrals =
    {
        ("Corral_Vacas", FarmAnimalKind.Cow),
        ("Corral_Pollos", FarmAnimalKind.Chicken),
        ("Corral_Cerdos", FarmAnimalKind.Pig),
    };

    public static void Run()
    {
        HideObsoleteHudNodes();
        EnsureNamedCorralsHaveZones();

        var shop = Object.FindFirstObjectByType<ShopSystem>();
        var inv = Object.FindFirstObjectByType<InventorySystem>();

        // GameObject.Find no ve objetos desactivados; ShopPanel suele empezar oculto → había que buscar incluyendo inactivos.
        var panelGo = FindSceneObjectByNameIncludingInactive("ShopPanel");
        ShopUI shopUi = null;

        if (panelGo != null && shop != null && inv != null)
        {
            shopUi = panelGo.GetComponent<ShopUI>() ?? panelGo.AddComponent<ShopUI>();
            shopUi.BindScene(panelGo, shop, inv);
            RetitleShopButtons(panelGo.transform);
        }
        else
        {
            shopUi = FindFirstShopUiIncludingInactive();
        }

        var uiMgr = Object.FindFirstObjectByType<UIManager>();
        if (uiMgr != null && shopUi != null)
            uiMgr.RegisterShopUi(shopUi);

        EnsureGraneroClickArea(shopUi);
        EnsureShopLivestockSystems();
    }

    private static GameObject FindSceneObjectByNameIncludingInactive(string objectName)
    {
        var direct = GameObject.Find(objectName);
        if (direct != null)
            return direct;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t.gameObject.scene.IsValid() || t.name != objectName)
                continue;
            return t.gameObject;
        }

        return null;
    }

    private static ShopUI FindFirstShopUiIncludingInactive()
    {
        var list = Object.FindObjectsByType<ShopUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return list.Length > 0 ? list[0] : null;
    }

    private static void EnsureNamedCorralsHaveZones()
    {
        foreach (var entry in NamedCorrals)
        {
            var go = GameObject.Find(entry.ObjectName);
            if (go == null)
                continue;

            EnsureInteriorZoneChildWithCollider(go, entry.Kind);

            if (go.GetComponent<Collider2D>() == null && !HasChildInteriorZoneCollider(go))
                AddDefaultTriggerCollider(go);

            var zone = go.GetComponent<CorralZone>();
            if (zone == null)
                zone = go.AddComponent<CorralZone>();
            else
                zone.RefreshAreaReference();

            zone.ApplyKind(entry.Kind);
        }
    }

    private static bool HasChildInteriorZoneCollider(GameObject corralRoot)
    {
        for (var i = 0; i < corralRoot.transform.childCount; i++)
        {
            var ch = corralRoot.transform.GetChild(i);
            var nl = ch.name.ToLowerInvariant();
            if ((nl.Contains("zona") || nl.Contains("sona")) && ch.GetComponent<Collider2D>() != null)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Prefiere una zona ya colocada en la escena (nombre exacto o cualquier hijo con "zona"/"sona" en el nombre).
    /// Solo crea <see cref="InteriorChildName"/> si no hay ningún candidato — evita duplicar zonas manuales con otros nombres.
    /// </summary>
    private static Transform ResolveInteriorZoneTransform(Transform corralRoot, FarmAnimalKind kind)
    {
        var exact = InteriorChildName(kind);
        var tr = corralRoot.Find(exact);
        if (tr != null)
            return tr;

        for (var i = 0; i < corralRoot.childCount; i++)
        {
            var ch = corralRoot.GetChild(i);
            var nl = ch.name.ToLowerInvariant();
            if (nl.Contains("zona") || nl.Contains("sona"))
                return ch;
        }

        return null;
    }

    private static void EnsureInteriorZoneChildWithCollider(GameObject corralRootGo, FarmAnimalKind kind)
    {
        var corralRoot = corralRootGo.transform;
        var interiorTf = ResolveInteriorZoneTransform(corralRoot, kind);

        GameObject zonaGo;
        if (interiorTf == null)
        {
            var interiorName = InteriorChildName(kind);
            zonaGo = new GameObject(interiorName);
            zonaGo.transform.SetParent(corralRoot, false);
            zonaGo.transform.localPosition = Vector3.zero;
            zonaGo.transform.localRotation = Quaternion.identity;
            zonaGo.transform.localScale = Vector3.one;
        }
        else
        {
            zonaGo = interiorTf.gameObject;
        }

        if (zonaGo.GetComponent<Collider2D>() != null)
            return;

        var box = zonaGo.AddComponent<BoxCollider2D>();
        FitInteriorBoxFromCorralVisuals(corralRoot, zonaGo.transform, box);
    }

    private static string InteriorChildName(FarmAnimalKind kind)
    {
        return kind switch
        {
            FarmAnimalKind.Cow => "zonaVacas",
            FarmAnimalKind.Chicken => "zonaPollos",
            FarmAnimalKind.Pig => "zonaCerdos",
            _ => "InteriorZone"
        };
    }

    private static void FitInteriorBoxFromCorralVisuals(Transform corralRoot, Transform zonaTf, BoxCollider2D box)
    {
        box.isTrigger = true;

        var union = ComputeVisualBoundsExcludingInterior(corralRoot, zonaTf);
        if (!union.HasValue)
        {
            box.offset = Vector2.zero;
            box.size = new Vector2(5f, 4f);
            return;
        }

        var bounds = union.Value;
        var centerLocal = zonaTf.InverseTransformPoint(bounds.center);
        var dx = zonaTf.InverseTransformVector(new Vector3(bounds.extents.x, 0f, 0f)).magnitude * 2f;
        var dy = zonaTf.InverseTransformVector(new Vector3(0f, bounds.extents.y, 0f)).magnitude * 2f;
        box.offset = new Vector2(centerLocal.x, centerLocal.y);
        box.size = new Vector2(
            Mathf.Max(0.75f, dx * 0.88f),
            Mathf.Max(0.75f, dy * 0.88f));
    }

    private static Bounds? ComputeVisualBoundsExcludingInterior(Transform corralRoot, Transform zonaTf)
    {
        Bounds? merged = null;

        foreach (var r in corralRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (zonaTf != null && (r.transform == zonaTf || r.transform.IsChildOf(zonaTf)))
                continue;

            var b = r.bounds;
            if (merged == null)
                merged = b;
            else
            {
                var bb = merged.Value;
                bb.Encapsulate(b);
                merged = bb;
            }
        }

        return merged;
    }

    private static void AddDefaultTriggerCollider(GameObject go)
    {
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            var b = sr.sprite.bounds;
            box.offset = new Vector2(b.center.x, b.center.y);
            box.size = new Vector2(Mathf.Abs(b.size.x), Mathf.Abs(b.size.y));
        }
        else
            box.size = new Vector2(6f, 5f);
    }

    private static void EnsureShopLivestockSystems()
    {
        var gs = GameObject.Find("GameSystems");
        if (gs == null) return;

        if (gs.GetComponent<AnimalSpawner>() == null)
            gs.AddComponent<AnimalSpawner>();

        if (gs.GetComponent<CorralManager>() == null)
            gs.AddComponent<CorralManager>();

        if (gs.GetComponent<PowerUpSystem>() == null)
            gs.AddComponent<PowerUpSystem>();

        if (gs.GetComponent<DayNightManager>() == null)
            gs.AddComponent<DayNightManager>();
    }

    private static void HideObsoleteHudNodes()
    {
        HideIfPresent("GoldText");
        HideIfPresent("WoodText");
        HideIfPresent("StoneText");
    }

    private static void HideIfPresent(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            go.SetActive(false);
    }

    private static void RetitleShopButtons(Transform panel)
    {
        TryRetitle(panel, "SellCow", "Mejorar ataque");
        TryRetitle(panel, "SellChicken", "Mejorar velocidad");
        TryRetitle(panel, "SellPig", "Cerrar granero");
    }

    private static void TryRetitle(Transform panel, string childName, string label)
    {
        var child = panel.Find(childName);
        if (child == null) return;
        var tx = child.GetComponentInChildren<Text>(true);
        if (tx != null)
            tx.text = label;
    }

    private static void EnsureGraneroClickArea(ShopUI shopUi)
    {
        var barn = FindSceneObjectByNameIncludingInactive("Granero");
        if (barn == null)
            return;

        var sr = barn.GetComponent<SpriteRenderer>() ?? barn.GetComponentInChildren<SpriteRenderer>(true);

        var addedNewCollider = !barn.TryGetComponent<Collider2D>(out var clickArea) || clickArea == null;
        if (addedNewCollider)
            clickArea = barn.gameObject.AddComponent<BoxCollider2D>();

        if (clickArea == null)
            return;

        // Solo dimensionamos el BoxCollider cuando lo creamos nosotros; si lo pusiste a mano en la escena, no lo tocamos.
        if (addedNewCollider && clickArea is BoxCollider2D boxNew)
        {
            clickArea.isTrigger = false;
            if (sr != null && sr.sprite != null)
            {
                var b = sr.sprite.bounds;
                boxNew.offset = new Vector2(b.center.x, b.center.y);
                boxNew.size = new Vector2(Mathf.Abs(b.size.x), Mathf.Abs(b.size.y));
            }
            else
            {
                boxNew.offset = Vector2.zero;
                boxNew.size = new Vector2(4f, 3f);
            }
        }
        else if (clickArea is BoxCollider2D boxScene && boxScene.size.x <= 1.05f && boxScene.size.y <= 1.05f)
        {
            // Caja por defecto 1×1 en la escena: alinear al sprite visible (suele estar en un hijo).
            TryFitGraneroBoxToChildRenderers(barn, boxScene);
        }

        var trig = barn.GetComponent<BarnShopTrigger>() ?? barn.AddComponent<BarnShopTrigger>();
        trig.BindShopUiIfProvided(shopUi);
    }

    private static void TryFitGraneroBoxToChildRenderers(GameObject barn, BoxCollider2D box)
    {
        Bounds? merged = null;
        foreach (var r in barn.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled)
                continue;
            if (merged == null)
                merged = r.bounds;
            else
            {
                var mb = merged.Value;
                mb.Encapsulate(r.bounds);
                merged = mb;
            }
        }

        if (!merged.HasValue)
            return;

        var wb = merged.Value;
        var z = barn.transform.position.z;
        var corners = new[]
        {
            new Vector3(wb.min.x, wb.min.y, z),
            new Vector3(wb.max.x, wb.min.y, z),
            new Vector3(wb.min.x, wb.max.y, z),
            new Vector3(wb.max.x, wb.max.y, z),
        };

        var lxMin = float.PositiveInfinity;
        var lxMax = float.NegativeInfinity;
        var lyMin = float.PositiveInfinity;
        var lyMax = float.NegativeInfinity;
        foreach (var c in corners)
        {
            var lp = barn.transform.InverseTransformPoint(c);
            lxMin = Mathf.Min(lxMin, lp.x);
            lxMax = Mathf.Max(lxMax, lp.x);
            lyMin = Mathf.Min(lyMin, lp.y);
            lyMax = Mathf.Max(lyMax, lp.y);
        }

        box.isTrigger = false;
        box.offset = new Vector2((lxMin + lxMax) * 0.5f, (lyMin + lyMax) * 0.5f);
        box.size = new Vector2(
            Mathf.Max(0.25f, lxMax - lxMin),
            Mathf.Max(0.25f, lyMax - lyMin));
    }
}
