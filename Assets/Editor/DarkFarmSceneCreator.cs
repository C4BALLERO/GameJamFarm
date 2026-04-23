using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DarkFarmSceneCreator
{
    private const string SceneMainPath = "Assets/scene_00/scene_00_main.unity";
    private const string SceneMenuPath = "Assets/scene_00/scene_01_menu.unity";
    private const string SceneTestPath = "Assets/scene_00/scene_02_test.unity";

    private const string PrefabPlayerPath = "Assets/prefabs_02/player/player.prefab";
    private const string PrefabEnemyMeleePath = "Assets/prefabs_02/enemies/plant_basic.prefab";
    private const string PrefabEnemyRangedPath = "Assets/prefabs_02/enemies/plant_ranged.prefab";
    private const string PrefabProjectilePath = "Assets/prefabs_02/enemies/projectile.prefab";
    private const string PrefabCowPath = "Assets/prefabs_02/animals/cow.prefab";
    private const string PrefabChickenPath = "Assets/prefabs_02/animals/chicken.prefab";
    private const string PrefabPigPath = "Assets/prefabs_02/animals/pig.prefab";
    private const string PrefabUiPath = "Assets/prefabs_02/ui/ui_root.prefab";

    [MenuItem("Tools/DarkFarm/Generate Prefabs + Scenes (MVP)")]
    public static void GenerateAll()
    {
        GeneratePrefabs();
        CreateMainSceneFromPrefabs();
        CreateMenuScene();
        CreateTestScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void GeneratePrefabs()
    {
        EnsureFolders();

        // Player prefab
        {
            var player = CreateCapsuleEntity("Player", "Player", Vector2.zero, new Color(0.22f, 0.25f, 0.22f, 1f));
            var prb = player.GetComponent<Rigidbody2D>();
            prb.gravityScale = 0f;
            prb.freezeRotation = true;
            prb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var pHealth = player.AddComponent<PlayerHealth>();

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(player.transform, false);
            var anim = visuals.AddComponent<Animator>();
            var psr = visuals.AddComponent<SpriteRenderer>();
            psr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            psr.color = new Color(0.18f, 0.18f, 0.2f, 1f);
            psr.sortingOrder = 5;

            var hitboxGo = new GameObject("Hitbox");
            hitboxGo.transform.SetParent(player.transform, false);
            var hitCol = hitboxGo.AddComponent<BoxCollider2D>();
            hitCol.isTrigger = true;
            hitCol.size = new Vector2(0.8f, 0.8f);

            var combat = player.AddComponent<PlayerCombat>();
            Assign(combat, "hitboxCollider", hitCol);
            Assign(combat, "enemyLayers", LayerMask.GetMask("Enemy"));

            var controller = player.AddComponent<PlayerController>();
            Assign(controller, "rb", prb);
            Assign(controller, "combat", combat);
            Assign(controller, "health", pHealth);
            Assign(controller, "animator", anim);

            SaveAsPrefabAndDestroy(player, PrefabPlayerPath);
        }

        // Projectile prefab
        {
            var proj = CreateProjectileProto("Projectile");
            SaveAsPrefabAndDestroy(proj, PrefabProjectilePath);
        }

        // Enemy prefabs
        {
            var melee = CreateEnemyProto("PlantEnemy", typeof(PlantEnemy), Vector2.zero, new Color(0.16f, 0.33f, 0.18f, 1f));
            SaveAsPrefabAndDestroy(melee, PrefabEnemyMeleePath);

            var ranged = CreateEnemyProto("RangedPlant", typeof(RangedPlant), Vector2.zero, new Color(0.25f, 0.12f, 0.28f, 1f));
            var rangedComp = ranged.GetComponent<RangedPlant>();
            Assign(rangedComp, "projectilePrefab", AssetDatabase.LoadAssetAtPath<Projectile>(PrefabProjectilePath));
            Assign(rangedComp, "hitLayers", LayerMask.GetMask("Player", "Animal"));
            SaveAsPrefabAndDestroy(ranged, PrefabEnemyRangedPath);
        }

        // Animal prefabs
        {
            var cow = CreateAnimalPrefab("Cow", new Color(0.36f, 0.33f, 0.28f, 1f), ResourceType.Meat, 2, 7f);
            SaveAsPrefabAndDestroy(cow, PrefabCowPath);

            var chicken = CreateAnimalPrefab("Chicken", new Color(0.42f, 0.38f, 0.33f, 1f), ResourceType.Blood, 1, 4.5f);
            SaveAsPrefabAndDestroy(chicken, PrefabChickenPath);

            var pig = CreateAnimalPrefab("Pig", new Color(0.33f, 0.28f, 0.28f, 1f), ResourceType.Essence, 1, 8.5f);
            SaveAsPrefabAndDestroy(pig, PrefabPigPath);
        }

        // UI prefab (optional but requested)
        {
            CreateUI(out var uiManager, out var shopPanel);
            var root = uiManager.gameObject.transform.parent.gameObject; // Canvas
            SaveAsPrefabAndDestroy(root, PrefabUiPath);
        }
    }

    private static void CreateMainSceneFromPrefabs()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        EnsureLayers();

        // _Game root
        var gameRoot = new GameObject("_Game");
        var time = gameRoot.AddComponent<TimeManager>();
        var inventory = gameRoot.AddComponent<InventorySystem>();
        var gm = gameRoot.AddComponent<GameManager>();

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);

        // Instantiate Player prefab
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPlayerPath);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.name = "Player";
        player.transform.position = new Vector2(0f, -1f);
        var pHealth = player.GetComponent<PlayerHealth>();

        // Spawner
        var spawnerGo = new GameObject("SpawnManager");
        var spawner = spawnerGo.AddComponent<SpawnManager>();
        Assign(spawner, "player", player.transform);
        Assign(spawner, "timeManager", time);
        Assign(spawner, "enemyPrefabs", new EnemyBase[]
        {
            AssetDatabase.LoadAssetAtPath<EnemyBase>(PrefabEnemyMeleePath),
            AssetDatabase.LoadAssetAtPath<EnemyBase>(PrefabEnemyRangedPath)
        });

        // Animals root
        var animalsRoot = new GameObject("_Animals").transform;

        // Instantiate animal prefabs and init generators
        InstantiateAnimal(PrefabCowPath, new Vector2(-2.5f, 1.5f), inventory, animalsRoot);
        InstantiateAnimal(PrefabChickenPath, new Vector2(-1.5f, 2.2f), inventory, animalsRoot);
        InstantiateAnimal(PrefabPigPath, new Vector2(-3.3f, 2.2f), inventory, animalsRoot);

        // ShopSystem (MVP uses prefab references; we'll leave nulls so you can assign prefabs later)
        var shopGo = new GameObject("ShopSystem");
        var shop = shopGo.AddComponent<ShopSystem>();
        Assign(shop, "inventory", inventory);
        Assign(shop, "animalSpawnRoot", animalsRoot);
        Assign(shop, "cowPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PrefabCowPath));
        Assign(shop, "chickenPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PrefabChickenPath));
        Assign(shop, "pigPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPigPath));

        // UI
        var uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUiPath);
        var uiRoot = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab);
        var uiManager = uiRoot.GetComponentInChildren<UIManager>(true);
        var shopPanel = uiRoot.transform.Find("ShopPanel") != null ? uiRoot.transform.Find("ShopPanel").gameObject : null;
        Assign(uiManager, "shopSystem", shop);
        Assign(uiManager, "shopPanelRoot", shopPanel);
        uiManager.Bind(pHealth, inventory);

        // Save scene
        EditorSceneManager.SaveScene(scene, SceneMainPath);
        EditorSceneManager.OpenScene(SceneMainPath, OpenSceneMode.Single);
        Selection.activeGameObject = player;
        Debug.Log($"Main scene created at {SceneMainPath}. Press Play.");
    }

    private static GameObject CreateCapsuleEntity(string name, string layerName, Vector2 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        var layer = LayerMask.NameToLayer(layerName);
        if (layer != -1) go.layer = layer;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Vertical;
        col.size = new Vector2(0.6f, 1.0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        sr.color = color;
        sr.sortingOrder = 5;

        return go;
    }

    private static GameObject CreateEnemyProto(string name, System.Type enemyType, Vector2 pos, Color color)
    {
        var go = CreateCapsuleEntity(name, "Enemy", pos, color);
        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // visuals + animator placeholder
        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(go.transform, false);
        visuals.AddComponent<Animator>();
        var sr = visuals.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        sr.color = color;
        sr.sortingOrder = 5;

        var enemy = (EnemyBase)go.AddComponent(enemyType);
        var ai = go.AddComponent<EnemyAI>();
        Assign(ai, "enemy", enemy);
        Assign(ai, "animator", visuals.GetComponent<Animator>());

        return go;
    }

    private static GameObject CreateProjectileProto(string name)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("Enemy");

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        sr.color = new Color(0.55f, 0.15f, 0.65f, 1f);
        sr.sortingOrder = 6;

        go.AddComponent<Projectile>();
        return go;
    }

    private static void CreateAnimal(string name, Vector2 pos, Color color, ResourceType type, int amount, float seconds, InventorySystem inv, Transform parent)
    {
        var go = CreateCapsuleEntity(name, "Animal", pos, color);
        go.transform.SetParent(parent, true);

        var rb = go.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        go.AddComponent<AnimalPlaceholder>();

        var gen = go.AddComponent<ResourceGenerator>();
        Assign(gen, "resourceType", type);
        Assign(gen, "amountPerTick", amount);
        Assign(gen, "secondsPerTick", seconds);
        gen.Init(inv);
    }

    private static GameObject CreateAnimalPrefab(string name, Color color, ResourceType type, int amount, float seconds)
    {
        var go = CreateCapsuleEntity(name, "Animal", Vector2.zero, color);
        var rb = go.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        go.AddComponent<AnimalPlaceholder>();
        var gen = go.AddComponent<ResourceGenerator>();
        Assign(gen, "resourceType", type);
        Assign(gen, "amountPerTick", amount);
        Assign(gen, "secondsPerTick", seconds);
        return go;
    }

    private static void InstantiateAnimal(string prefabPath, Vector2 pos, InventorySystem inv, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        if (go.TryGetComponent<ResourceGenerator>(out var gen))
            gen.Init(inv);
    }

    private static void SaveAsPrefabAndDestroy(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/scene_00")) AssetDatabase.CreateFolder("Assets", "scene_00");
        if (!AssetDatabase.IsValidFolder("Assets/prefabs_02")) AssetDatabase.CreateFolder("Assets", "prefabs_02");
        if (!AssetDatabase.IsValidFolder("Assets/prefabs_02/player")) AssetDatabase.CreateFolder("Assets/prefabs_02", "player");
        if (!AssetDatabase.IsValidFolder("Assets/prefabs_02/enemies")) AssetDatabase.CreateFolder("Assets/prefabs_02", "enemies");
        if (!AssetDatabase.IsValidFolder("Assets/prefabs_02/animals")) AssetDatabase.CreateFolder("Assets/prefabs_02", "animals");
        if (!AssetDatabase.IsValidFolder("Assets/prefabs_02/ui")) AssetDatabase.CreateFolder("Assets/prefabs_02", "ui");
    }

    private static void CreateMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.035f, 1f);

        // Minimal canvas with title text
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(canvasGo.transform, false);
        var text = titleGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = "DARK FARM (MVP)\nPress Play in Main Scene";
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var rt = titleGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.4f);
        rt.anchorMax = new Vector2(0.8f, 0.7f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        EditorSceneManager.SaveScene(scene, SceneMenuPath);
    }

    private static void CreateTestScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.07f, 1f);

        // Drop one of each prefab for quick testing
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPlayerPath);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector2(-2f, 0f);

        var meleePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEnemyMeleePath);
        var melee = (GameObject)PrefabUtility.InstantiatePrefab(meleePrefab);
        melee.transform.position = new Vector2(2f, 0.5f);
        melee.GetComponent<EnemyAI>()?.SetTarget(player.transform);

        var rangedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEnemyRangedPath);
        var ranged = (GameObject)PrefabUtility.InstantiatePrefab(rangedPrefab);
        ranged.transform.position = new Vector2(3.5f, -0.5f);
        ranged.GetComponent<EnemyAI>()?.SetTarget(player.transform);

        EditorSceneManager.SaveScene(scene, SceneTestPath);
    }

    private static void CreateUI(out UIManager uiManager, out GameObject shopPanelRoot)
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // HUD
        var hudGo = new GameObject("UIManager");
        hudGo.transform.SetParent(canvasGo.transform, false);
        uiManager = hudGo.AddComponent<UIManager>();

        // Health slider
        var sliderGo = new GameObject("HealthBar");
        sliderGo.transform.SetParent(hudGo.transform, false);
        var slider = sliderGo.AddComponent<Slider>();
        var srt = sliderGo.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.02f, 0.93f);
        srt.anchorMax = new Vector2(0.32f, 0.98f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;

        var healthBar = sliderGo.AddComponent<HealthBar>();
        Assign(healthBar, "slider", slider);
        Assign(uiManager, "healthBar", healthBar);

        // Resource UI (texts)
        var resGo = new GameObject("ResourceUI");
        resGo.transform.SetParent(hudGo.transform, false);
        var res = resGo.AddComponent<ResourceUI>();
        Assign(uiManager, "resourceUI", res);

        // Simple shop panel (toggle with Tab)
        shopPanelRoot = new GameObject("ShopPanel");
        shopPanelRoot.transform.SetParent(canvasGo.transform, false);
        var img = shopPanelRoot.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.7f);
        var rt = shopPanelRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.65f, 0.2f);
        rt.anchorMax = new Vector2(0.98f, 0.8f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        shopPanelRoot.SetActive(false);
    }

    private static void Assign(Object obj, string fieldName, object value)
    {
        if (obj == null) return;
        var so = new SerializedObject(obj);
        var prop = so.FindProperty(fieldName);
        if (prop == null) { so.Dispose(); return; }

        if (prop.propertyType == SerializedPropertyType.ObjectReference)
            prop.objectReferenceValue = value as Object;
        else if (prop.propertyType == SerializedPropertyType.Integer && value is int i)
            prop.intValue = i;
        else if (prop.propertyType == SerializedPropertyType.Float && value is float f)
            prop.floatValue = f;
        else if (prop.propertyType == SerializedPropertyType.Enum && value != null)
            prop.enumValueIndex = (int)value;
        else if (prop.propertyType == SerializedPropertyType.LayerMask && value is LayerMask lm)
            prop.intValue = lm.value;
        else if (prop.propertyType == SerializedPropertyType.Generic && prop.isArray && value is System.Array arr)
        {
            prop.arraySize = arr.Length;
            for (var idx = 0; idx < arr.Length; idx++)
            {
                var element = prop.GetArrayElementAtIndex(idx);
                element.objectReferenceValue = arr.GetValue(idx) as Object;
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        so.Dispose();
    }

    private static void EnsureLayers()
    {
        // Optional: keeping it empty avoids touching ProjectSettings automatically.
        // If you want, create layers manually: Player, Enemy, Animal.
    }

    // Minimal animal component to satisfy "AnimalBase" in your structure later if you want concrete classes.
    private sealed class AnimalPlaceholder : AnimalBase { }
}

