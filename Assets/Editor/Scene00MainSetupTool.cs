using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public static class Scene00MainSetupTool
{
    private const string ScenePath = "Assets/scene_00/scene_00_main.unity";

    [MenuItem("Tools/DarkFarm/Apply Scene00 Main Setup")]
    public static void ApplyScene00Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("[Scene00Setup] Could not open scene_00_main.");
            return;
        }

        var gameSystems = FindSceneObjectByName("GameSystems") ?? FindHostByComponent<ShopSystem>() ?? new GameObject("GameSystems");
        var spawnGo = FindSceneObjectByName("SpawnManager") ?? FindHostByComponent<SpawnManager>() ?? new GameObject("SpawnManager");
        var shopPanel = FindSceneObjectByName("ShopPanel") ?? CreateShopPanelUnderCanvas();
        var uiManagerGo = FindSceneObjectByName("UIManager") ?? FindHostByComponent<UIManager>() ?? CreateUiManagerUnderCanvas();

        if (shopPanel == null)
        {
            Debug.LogError("[Scene00Setup] Could not resolve or create ShopPanel.");
            return;
        }

        if (uiManagerGo == null)
        {
            Debug.LogError("[Scene00Setup] Could not resolve or create UIManager.");
            return;
        }

        var shop = GetOrAdd<ShopSystem>(gameSystems);
        var inventory = GetOrAdd<InventorySystem>(gameSystems);
        var time = GetOrAdd<TimeManager>(gameSystems);
        var gameManager = GetOrAdd<GameManager>(gameSystems);
        var corralManager = GetOrAdd<CorralManager>(gameSystems);
        var animalSpawner = GetOrAdd<AnimalSpawner>(gameSystems);
        var powerUpSystem = GetOrAdd<PowerUpSystem>(gameSystems);
        var dayNightManager = GetOrAdd<DayNightManager>(gameSystems);

        var shopUi = GetOrAdd<ShopUI>(shopPanel);
        var uiManager = GetOrAdd<UIManager>(uiManagerGo);
        var spawnManager = GetOrAdd<SpawnManager>(spawnGo);
        var dayNightHud = EnsureDayNightHud();
        EnsureResourceHudIcons();
        var pumpkinPrefab = EnsurePumpkinEnemyPrefab();
        EnsureRoseDamageIsBalanced();

        SerializedSet(shop, "inventory", inventory);
        SerializedSet(shop, "animalSpawner", animalSpawner);
        SerializedSet(shop, "powerUpSystem", powerUpSystem);

        SerializedSet(gameManager, "timeManager", time);
        SerializedSet(gameManager, "inventorySystem", inventory);

        SerializedSet(spawnManager, "timeManager", time);
        SerializedSet(spawnManager, "dayNightManager", dayNightManager);
        SerializedSet(spawnManager, "corralManager", corralManager);
        AddEnemyPrefabToSpawnList(spawnManager, pumpkinPrefab);

        SerializedSet(shopUi, "panelRoot", shopPanel);
        SerializedSet(shopUi, "shopSystem", shop);
        SerializedSet(shopUi, "inventory", inventory);
        EnsureShopCostLabels(shopPanel, shopUi);

        SerializedSet(uiManager, "shopSystem", shop);
        SerializedSet(uiManager, "shopUi", shopUi);
        SerializedSet(uiManager, "shopPanelRoot", shopPanel);

        var phaseTextGo = FindSceneObjectByName("DayNightText");
        if (phaseTextGo != null && phaseTextGo.TryGetComponent<Text>(out var phaseText))
            SerializedSet(dayNightManager, "phaseText", phaseText);
        if (dayNightHud != null)
        {
            var sun = FindChildComponentByName<Image>(dayNightHud.transform, "SunIcon");
            var moon = FindChildComponentByName<Image>(dayNightHud.transform, "MoonIcon");
            var fill = FindChildComponentByName<Image>(dayNightHud.transform, "ClockFill");
            var hand = FindChildComponentByName<RectTransform>(dayNightHud.transform, "ClockHand");
            var txt = FindChildComponentByName<Text>(dayNightHud.transform, "ClockText");
            SerializedSet(dayNightManager, "sunIcon", sun);
            SerializedSet(dayNightManager, "moonIcon", moon);
            SerializedSet(dayNightManager, "clockFill", fill);
            SerializedSet(dayNightManager, "clockHand", hand);
            SerializedSet(dayNightManager, "clockText", txt);
        }

        var globalLight = FindGlobalLight2D();
        if (globalLight != null)
            SerializedSet(dayNightManager, "globalLight2D", globalLight);

        EditorUtility.SetDirty(gameSystems);
        EditorUtility.SetDirty(spawnGo);
        EditorUtility.SetDirty(shopPanel);
        EditorUtility.SetDirty(uiManagerGo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[Scene00Setup] scene_00_main configured and saved (auto-create enabled).");
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        if (go.TryGetComponent<T>(out var c))
            return c;
        return go.AddComponent<T>();
    }

    private static void SerializedSet(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var sp = so.FindProperty(fieldName);
        if (sp == null || sp.propertyType != SerializedPropertyType.ObjectReference)
            return;
        sp.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddEnemyPrefabToSpawnList(SpawnManager spawnManager, EnemyBase pumpkinPrefab)
    {
        if (spawnManager == null || pumpkinPrefab == null)
            return;

        var so = new SerializedObject(spawnManager);
        var arr = so.FindProperty("enemyPrefabs");
        if (arr == null || !arr.isArray)
            return;

        var exists = false;
        for (var i = 0; i < arr.arraySize; i++)
        {
            var elem = arr.GetArrayElementAtIndex(i);
            if (elem.objectReferenceValue == pumpkinPrefab)
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            arr.InsertArrayElementAtIndex(arr.arraySize);
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = pumpkinPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Scene00Setup] Added pumpkin enemy prefab to SpawnManager list.");
        }
    }

    private static void EnsureShopCostLabels(GameObject shopPanel, ShopUI shopUi)
    {
        if (shopPanel == null || shopUi == null)
            return;

        var titleY = 0.93f;
        var step = 0.065f;
        var labels = new[]
        {
            ("CowCostText", "Vaca", "Assets/resources/vacaIcono.png"),
            ("ChickenCostText", "Gallina", "Assets/resources/polloIcono.png"),
            ("PigCostText", "Cerdo", "Assets/resources/cerdoIcono.png"),
            ("AttackCostText", "Mejora ataque", "Assets/resources/CarneGota.png"),
            ("SpeedCostText", "Mejora velocidad", "Assets/resources/HuevoGota.png"),
            ("FasterGenerationCostText", "PU Produccion", "Assets/resources/LecheGota.png"),
            ("AnimalHealthCostText", "PU Vida Animal", "Assets/resources/CarneGota.png"),
            ("PlayerDamagePowerCostText", "PU Danio", "Assets/resources/CarneGota.png"),
            ("PlayerMovePowerCostText", "PU Movimiento", "Assets/resources/HuevoGota.png"),
            ("ReducedSpawnDelayCostText", "PU Spawn", "Assets/resources/Luna.png"),
            ("CurrentResourcesText", "Recursos", "Assets/resources/Sol.png")
        };

        var created = new List<Text>();
        for (var i = 0; i < labels.Length; i++)
        {
            var (name, seedText, iconPath) = labels[i];
            var txt = FindChildComponentByName<Text>(shopPanel.transform, name);
            if (txt == null)
            {
                txt = CreateHudText(shopPanel.transform, name, new Vector2(0.12f, titleY - step * (i + 1)), new Vector2(0.62f, titleY - step * i), seedText);
                txt.fontSize = 14;
                txt.alignment = TextAnchor.MiddleLeft;
            }
            else if (txt.transform is RectTransform txrt)
            {
                txrt.anchorMin = new Vector2(0.12f, titleY - step * (i + 1));
                txrt.anchorMax = new Vector2(0.62f, titleY - step * i);
                txrt.offsetMin = Vector2.zero;
                txrt.offsetMax = Vector2.zero;
            }
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 11;
            txt.resizeTextMaxSize = 16;
            txt.color = new Color(0.95f, 0.95f, 0.98f, 1f);

            EnsureShopRowIcon(shopPanel.transform, $"Icon_{name}", titleY - step * (i + 1), titleY - step * i, iconPath);
            created.Add(txt);
        }

        LayoutShopButtons(shopPanel.transform);

        var so = new SerializedObject(shopUi);
        SetTextRef(so, "cowCostText", created[0]);
        SetTextRef(so, "chickenCostText", created[1]);
        SetTextRef(so, "pigCostText", created[2]);
        SetTextRef(so, "attackCostText", created[3]);
        SetTextRef(so, "speedCostText", created[4]);
        SetTextRef(so, "fasterGenerationCostText", created[5]);
        SetTextRef(so, "animalHealthCostText", created[6]);
        SetTextRef(so, "playerDamagePowerCostText", created[7]);
        SetTextRef(so, "playerMovePowerCostText", created[8]);
        SetTextRef(so, "reducedSpawnDelayCostText", created[9]);
        SetTextRef(so, "currentResourcesText", created[10]);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureShopRowIcon(Transform parent, string iconName, float minY, float maxY, string spriteAssetPath)
    {
        var icon = FindChildComponentByName<Image>(parent, iconName);
        if (icon == null)
            icon = CreateIconImage(parent, iconName, new Vector2(0.04f, minY), new Vector2(0.1f, maxY), Color.white);
        else if (icon.transform is RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.04f, minY);
            rt.anchorMax = new Vector2(0.1f, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        AssignIconSprite(icon, spriteAssetPath);
    }

    private static void LayoutShopButtons(Transform panel)
    {
        var rows = new[]
        {
            ("BuyCow", 0.86f, 0.93f, "Comprar vaca"),
            ("BuyChicken", 0.795f, 0.86f, "Comprar gallina"),
            ("BuyPig", 0.73f, 0.795f, "Comprar cerdo"),
            ("BuyAttackUpgrade", 0.665f, 0.73f, "Mejorar ataque"),
            ("BuySpeedUpgrade", 0.60f, 0.665f, "Mejorar velocidad"),
            ("CloseShop", 0.07f, 0.14f, "Cerrar granero"),
        };

        foreach (var (name, minY, maxY, label) in rows)
        {
            var btn = FindChildComponentByName<Button>(panel, name);
            if (btn == null) continue;
            if (btn.transform is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0.66f, minY);
                rt.anchorMax = new Vector2(0.96f, maxY);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var txt = btn.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.text = label;
                txt.resizeTextForBestFit = true;
                txt.resizeTextMinSize = 10;
                txt.resizeTextMaxSize = 20;
            }
        }
    }

    private static void SetTextRef(SerializedObject so, string propName, Text text)
    {
        var p = so.FindProperty(propName);
        if (p != null)
            p.objectReferenceValue = text;
    }

    private static GameObject FindHostByComponent<T>() where T : Component
    {
        var c = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        return c != null ? c.gameObject : null;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        var direct = GameObject.Find(objectName);
        if (direct != null)
            return direct;

        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go == null || !go.scene.IsValid())
                continue;
            if (go.name == objectName)
                return go;
        }

        return null;
    }

    private static GameObject CreateShopPanelUnderCanvas()
    {
        var canvas = FindSceneObjectByName("Canvas");
        if (canvas == null)
            return null;

        var go = new GameObject("ShopPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.SetActive(false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.55f, 0.15f);
        rect.anchorMax = new Vector2(0.98f, 0.85f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.08f, 0.07f, 0.12f, 0.92f);

        Debug.Log("[Scene00Setup] Created missing ShopPanel under Canvas.");
        return go;
    }

    private static GameObject CreateUiManagerUnderCanvas()
    {
        var canvas = FindSceneObjectByName("Canvas");
        if (canvas == null)
            return null;

        var go = new GameObject("UIManager", typeof(RectTransform), typeof(UIManager), typeof(ResourceUI));
        go.transform.SetParent(canvas.transform, false);
        Debug.Log("[Scene00Setup] Created missing UIManager under Canvas.");
        return go;
    }

    private static GameObject EnsureDayNightHud()
    {
        var canvas = FindSceneObjectByName("Canvas");
        if (canvas == null)
            return null;

        var root = FindSceneObjectByName("DayNightHud");
        if (root == null)
        {
            root = new GameObject("DayNightHud", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.79f, 0.88f);
            rt.anchorMax = new Vector2(0.98f, 0.99f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        var sun = FindChildComponentByName<Image>(root.transform, "SunIcon") ?? CreateIconImage(root.transform, "SunIcon", new Vector2(0.03f, 0.15f), new Vector2(0.21f, 0.85f), new Color(1f, 0.84f, 0.2f, 1f));
        var moon = FindChildComponentByName<Image>(root.transform, "MoonIcon") ?? CreateIconImage(root.transform, "MoonIcon", new Vector2(0.03f, 0.15f), new Vector2(0.21f, 0.85f), new Color(0.67f, 0.75f, 1f, 1f));
        var _ = FindChildComponentByName<Image>(root.transform, "ClockBg") ?? CreateIconImage(root.transform, "ClockBg", new Vector2(0.25f, 0.1f), new Vector2(0.52f, 0.9f), new Color(0f, 0f, 0f, 0.42f));
        var fill = FindChildComponentByName<Image>(root.transform, "ClockFill") ?? CreateIconImage(root.transform, "ClockFill", new Vector2(0.25f, 0.1f), new Vector2(0.52f, 0.9f), new Color(0.35f, 0.95f, 0.9f, 0.8f));
        var __ = FindChildComponentByName<Text>(root.transform, "ClockText") ?? CreateHudText(root.transform, "ClockText", new Vector2(0.55f, 0.1f), new Vector2(0.99f, 0.9f), "Day 120s");
        var ___ = FindChildComponentByName<RectTransform>(root.transform, "ClockHand") ?? CreateClockHand(root.transform);

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillClockwise = true;
        fill.fillOrigin = 2;
        fill.fillAmount = 0f;
        moon.enabled = false;
        sun.enabled = true;
        AssignIconSprite(sun, "Assets/resources/Sol.png");
        AssignIconSprite(moon, "Assets/resources/Luna.png");

        return root;
    }

    private static void EnsureResourceHudIcons()
    {
        var milkText = FindSceneObjectByName("MilkHud");
        var eggText = FindSceneObjectByName("EggHud");
        var meatText = FindSceneObjectByName("MeatHud");

        if (milkText != null)
            EnsureIconNearText(milkText.transform, "MilkIcon", "Assets/resources/LecheGota.png");
        if (eggText != null)
            EnsureIconNearText(eggText.transform, "EggIcon", "Assets/resources/HuevoGota.png");
        if (meatText != null)
            EnsureIconNearText(meatText.transform, "MeatIcon", "Assets/resources/CarneGota.png");
    }

    private static void EnsureIconNearText(Transform textTransform, string iconName, string spriteAssetPath)
    {
        var parent = textTransform.parent;
        if (parent == null)
            return;

        var icon = FindChildComponentByName<Image>(parent, iconName);
        if (icon == null)
        {
            // Bigger HUD icon, aligned to the left of each resource line.
            icon = CreateIconImage(parent, iconName, new Vector2(0.00f, 0.06f), new Vector2(0.22f, 0.94f), Color.white);
        }
        else if (icon.transform is RectTransform irt)
        {
            irt.anchorMin = new Vector2(0.00f, 0.06f);
            irt.anchorMax = new Vector2(0.22f, 0.94f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
        }

        // Shift the text right to make space for icon.
        if (textTransform is RectTransform rt)
        {
            var off = rt.offsetMin;
            off.x = Mathf.Max(off.x, 54f);
            rt.offsetMin = off;
        }

        AssignIconSprite(icon, spriteAssetPath);
    }

    private static void AssignIconSprite(Image image, string spriteAssetPath)
    {
        if (image == null)
            return;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
        if (sprite == null)
            return;

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    private static Image CreateIconImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text CreateHudText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string value)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = 16;
        t.alignment = TextAnchor.MiddleLeft;
        t.color = Color.white;
        t.text = value;
        return t;
    }

    private static RectTransform CreateClockHand(Transform parent)
    {
        var go = new GameObject("ClockHand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.385f, 0.5f);
        rt.anchorMax = new Vector2(0.385f, 0.5f);
        rt.sizeDelta = new Vector2(4f, 26f);
        rt.anchoredPosition = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0f);
        var img = go.GetComponent<Image>();
        img.color = Color.white;
        return rt;
    }

    private static T FindChildComponentByName<T>(Transform parent, string childName) where T : Component
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var ch = parent.GetChild(i);
            if (ch.name != childName)
                continue;
            if (ch.TryGetComponent<T>(out var c))
                return c;
        }
        return null;
    }

    private static UnityEngine.Object FindGlobalLight2D()
    {
        var lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (lightType == null)
            return null;

        var lights = Resources.FindObjectsOfTypeAll(lightType);
        foreach (var l in lights)
        {
            if (l is Component c && c.gameObject.scene.IsValid())
                return c;
        }

        return null;
    }

    private static EnemyBase EnsurePumpkinEnemyPrefab()
    {
        const string path = "Assets/prefabs_02/enemies/MvpPumpkinEnemy.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
        if (existing != null)
            return existing;

        var walk = LoadSpritesFromSheet("Assets/animations_04/enemies/CalabazaCamina.png");
        var death = LoadSpritesFromSheet("Assets/animations_04/enemies/CalabazaMuere.png");
        if (walk.Count == 0)
        {
            Debug.LogWarning("[Scene00Setup] Could not create pumpkin prefab (missing CalabazaCamina sprites).");
            return null;
        }

        var root = new GameObject("MvpPumpkinEnemy");
        root.layer = LayerMask.NameToLayer("Enemy");
        root.tag = "Enemy";
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = walk[0];
        sr.sortingOrder = 2;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(Mathf.Max(0.8f, walk[0].bounds.size.x * 0.5f), Mathf.Max(0.8f, walk[0].bounds.size.y * 0.48f));
        root.AddComponent<Animator>();
        var enemy = root.AddComponent<PumpkinEnemy>();
        var ai = root.AddComponent<EnemyAI>();
        var spriteAnim = root.AddComponent<EnemySpriteAnimator>();

        var enemySo = new SerializedObject(enemy);
        enemySo.FindProperty("damage").intValue = 2;
        enemySo.FindProperty("attackCooldown").floatValue = 1.05f;
        enemySo.ApplyModifiedPropertiesWithoutUndo();

        var aiSo = new SerializedObject(ai);
        aiSo.FindProperty("attackRange").floatValue = 1.1f;
        aiSo.FindProperty("moveSpeed").floatValue = 2.4f;
        aiSo.ApplyModifiedPropertiesWithoutUndo();

        FillEnemySpriteAnimator(spriteAnim, sr, walk, death);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Scene00Setup] Created MvpPumpkinEnemy prefab.");
        return prefab != null ? prefab.GetComponent<EnemyBase>() : AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
    }

    private static void EnsureRoseDamageIsBalanced()
    {
        const string rosePath = "Assets/prefabs_02/enemies/MvpRangedPlant.prefab";
        var rose = AssetDatabase.LoadAssetAtPath<RangedPlant>(rosePath);
        if (rose == null)
            return;

        var so = new SerializedObject(rose);
        var damageProp = so.FindProperty("damage");
        if (damageProp != null)
            damageProp.intValue = 1;
        var projOverride = so.FindProperty("projectileDamageOverride");
        if (projOverride != null)
            projOverride.intValue = 1;
        var minInterval = so.FindProperty("minProjectileAttackInterval");
        if (minInterval != null)
            minInterval.floatValue = 1.25f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<Sprite> LoadSpritesFromSheet(string path)
    {
        var outList = new List<Sprite>();
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
            if (a is Sprite s) outList.Add(s);
        outList.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return outList;
    }

    private static void FillEnemySpriteAnimator(EnemySpriteAnimator target, SpriteRenderer sr, List<Sprite> walk, List<Sprite> death)
    {
        var so = new SerializedObject(target);
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        var idle = so.FindProperty("idleFrames");
        idle.arraySize = walk.Count;
        for (var i = 0; i < walk.Count; i++)
            idle.GetArrayElementAtIndex(i).objectReferenceValue = walk[i];
        var walkArr = so.FindProperty("walkFrames");
        walkArr.arraySize = walk.Count;
        for (var i = 0; i < walk.Count; i++)
            walkArr.GetArrayElementAtIndex(i).objectReferenceValue = walk[i];
        var attack = so.FindProperty("attackFrames");
        attack.arraySize = walk.Count;
        for (var i = 0; i < walk.Count; i++)
            attack.GetArrayElementAtIndex(i).objectReferenceValue = walk[i];
        var deathArr = so.FindProperty("deathFrames");
        deathArr.arraySize = death.Count;
        for (var i = 0; i < death.Count; i++)
            deathArr.GetArrayElementAtIndex(i).objectReferenceValue = death[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}

