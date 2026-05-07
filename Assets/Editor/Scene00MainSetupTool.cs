using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
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
        var waveText = EnsureWaveCounterHud();
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
        SerializedSet(spawnManager, "waveCounterText", waveText);
        AddEnemyPrefabToSpawnList(spawnManager, pumpkinPrefab);

        SerializedSet(shopUi, "panelRoot", shopPanel);
        SerializedSet(shopUi, "shopSystem", shop);
        SerializedSet(shopUi, "inventory", inventory);
        EnsureShopCostLabels(shopPanel, shopUi);

        SerializedSet(uiManager, "shopSystem", shop);
        SerializedSet(uiManager, "shopUi", shopUi);
        SerializedSet(uiManager, "shopPanelRoot", shopPanel);

        // Wire ResourceUI + HUD icons
        var resourceUi = GetOrAdd<ResourceUI>(uiManagerGo);
        SerializedSet(uiManager, "resourceUI", resourceUi);
        EnsureResourceHud(uiManagerGo, resourceUi);

        // Barra de vida en HUD
        var hudHealthBar = EnsureHealthBarHud();
        SerializedSet(uiManager, "healthBar", hudHealthBar);

        // Pausa con ESC
        EnsurePauseMenu();

        // Drops de recursos al matar enemigos + sangre
        AddEnemyResourceDrop(pumpkinPrefab);
        AddBloodEffect(pumpkinPrefab);
        AddBloodEffectToAnimalPrefabs();

        // Perder recursos al morir
        GetOrAdd<PlayerDeathResourceLoss>(gameSystems);

        // Eliminar nodos HUD obsoletos de la escena
        CleanupObsoleteSceneNodes();

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

    // ─────────────────────────────────────────────────────────────────────────
    // SHOP PANEL – diseño 2 columnas:
    //   Izquierda → Animales + Jugador   |   Derecha → Power-Ups
    // ─────────────────────────────────────────────────────────────────────────

    private static void EnsureShopCostLabels(GameObject shopPanel, ShopUI shopUi)
    {
        if (shopPanel == null || shopUi == null) return;

        // Limpiar objetos del diseño anterior
        CleanLegacyShopChildren(shopPanel.transform);

        // Panel: ocupa casi toda la pantalla
        if (shopPanel.transform is RectTransform panelRt)
        {
            panelRt.anchorMin = new Vector2(0.04f, 0.03f);
            panelRt.anchorMax = new Vector2(0.96f, 0.97f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
        }
        var panelImg = shopPanel.GetComponent<Image>();
        if (panelImg != null)
            panelImg.color = new Color(0.07f, 0.06f, 0.11f, 0.96f);

        // ── Título ──────────────────────────────────────────────────────────
        EnsureHeaderAt(shopPanel.transform, "ShopTitle", "TIENDA",
            0.945f, 0.990f, 0.01f, 0.99f, 30, TextAnchor.MiddleCenter);

        // ── Barra de recursos (parte superior) ──────────────────────────────
        EnsureShopIcon(shopPanel.transform, "ResIconMilk",
            0.882f, 0.928f, 0.030f, 0.085f, "Assets/resources/LecheGota.png");
        EnsureShopIcon(shopPanel.transform, "ResIconEgg",
            0.882f, 0.928f, 0.135f, 0.190f, "Assets/resources/HuevoGota.png");
        EnsureShopIcon(shopPanel.transform, "ResIconMeat",
            0.882f, 0.928f, 0.240f, 0.295f, "Assets/resources/CarneGota.png");
        var resTxt = EnsureTextLabel(shopPanel.transform, "CurrentResourcesText",
            0.882f, 0.928f, 0.30f, 0.99f, "Leche: 0  Huevos: 0  Carne: 0");
        resTxt.alignment         = TextAnchor.MiddleRight;
        resTxt.color             = new Color(0.95f, 0.90f, 0.75f, 1f);
        resTxt.resizeTextForBestFit = true;
        resTxt.resizeTextMinSize = 11;
        resTxt.resizeTextMaxSize = 18;

        // ══ IZQUIERDA – ANIMALES ════════════════════════════════════════════
        EnsureHeaderAt(shopPanel.transform, "AnimalsHeader", "ANIMALES",
            0.836f, 0.873f, 0.01f, 0.49f, 17, TextAnchor.MiddleLeft);

        EnsureShopIcon(shopPanel.transform, "Icon_Cow",
            0.765f, 0.828f, 0.01f, 0.085f, "Assets/resources/vacaIcono.png");
        EnsureShopIcon(shopPanel.transform, "Icon_Chicken",
            0.695f, 0.758f, 0.01f, 0.085f, "Assets/resources/polloIcono.png");
        EnsureShopIcon(shopPanel.transform, "Icon_Pig",
            0.625f, 0.688f, 0.01f, 0.085f, "Assets/resources/cerdoIcono.png");

        var cowCost     = EnsureCostLabel(shopPanel.transform, "CowCostText",     0.765f, 0.828f, true);
        var chickenCost = EnsureCostLabel(shopPanel.transform, "ChickenCostText", 0.695f, 0.758f, true);
        var pigCost     = EnsureCostLabel(shopPanel.transform, "PigCostText",     0.625f, 0.688f, true);

        // ══ IZQUIERDA – JUGADOR ═════════════════════════════════════════════
        EnsureHeaderAt(shopPanel.transform, "PlayerHeader", "JUGADOR",
            0.576f, 0.613f, 0.01f, 0.49f, 17, TextAnchor.MiddleLeft);

        EnsureShopIcon(shopPanel.transform, "Icon_Attack",
            0.506f, 0.568f, 0.01f, 0.085f, "Assets/resources/CarneGota.png");
        EnsureShopIcon(shopPanel.transform, "Icon_Speed",
            0.436f, 0.498f, 0.01f, 0.085f, "Assets/resources/HuevoGota.png");
        EnsureShopIcon(shopPanel.transform, "Icon_Health",
            0.366f, 0.428f, 0.01f, 0.085f, "Assets/resources/Sol.png");

        var attackCost = EnsureCostLabel(shopPanel.transform, "AttackCostText",             0.506f, 0.568f, true);
        var speedCost  = EnsureCostLabel(shopPanel.transform, "SpeedCostText",              0.436f, 0.498f, true);
        var healthCost = EnsureCostLabel(shopPanel.transform, "PlayerHealthRestoreCostText", 0.366f, 0.428f, true);

        // ══ DERECHA – POWER UPS ═════════════════════════════════════════════
        EnsureHeaderAt(shopPanel.transform, "PowerHeader", "POWER UPS",
            0.836f, 0.873f, 0.51f, 0.99f, 17, TextAnchor.MiddleLeft);

        EnsureShopIcon(shopPanel.transform, "Icon_FasterGen",
            0.765f, 0.828f, 0.51f, 0.585f, "Assets/resources/LecheGota.png");
        EnsureShopIcon(shopPanel.transform, "Icon_AnimalHealth",
            0.695f, 0.758f, 0.51f, 0.585f, "Assets/resources/CarneGota.png");
        EnsureShopIcon(shopPanel.transform, "Icon_PlayerDmg",
            0.625f, 0.688f, 0.51f, 0.585f, "Assets/resources/CarneGota.png");
        EnsureShopIcon(shopPanel.transform, "Icon_PlayerMove",
            0.555f, 0.618f, 0.51f, 0.585f, "Assets/resources/HuevoGota.png");
        EnsureShopIcon(shopPanel.transform, "Icon_Spawn",
            0.485f, 0.548f, 0.51f, 0.585f, "Assets/resources/Luna.png");

        var fasterGenCost    = EnsureCostLabel(shopPanel.transform, "FasterGenerationCostText",  0.765f, 0.828f, false);
        var animalHealthCost = EnsureCostLabel(shopPanel.transform, "AnimalHealthCostText",      0.695f, 0.758f, false);
        var playerDmgCost    = EnsureCostLabel(shopPanel.transform, "PlayerDamagePowerCostText", 0.625f, 0.688f, false);
        var playerMoveCost   = EnsureCostLabel(shopPanel.transform, "PlayerMovePowerCostText",   0.555f, 0.618f, false);
        var spawnCost        = EnsureCostLabel(shopPanel.transform, "ReducedSpawnDelayCostText", 0.485f, 0.548f, false);

        // ── Botones ──────────────────────────────────────────────────────────
        LayoutShopButtons(shopPanel.transform, shopUi);

        // ── Asignar referencias a ShopUI ────────────────────────────────────
        var so = new SerializedObject(shopUi);
        SetTextRef(so, "cowCostText",                 cowCost);
        SetTextRef(so, "chickenCostText",             chickenCost);
        SetTextRef(so, "pigCostText",                 pigCost);
        SetTextRef(so, "attackCostText",              attackCost);
        SetTextRef(so, "speedCostText",               speedCost);
        SetTextRef(so, "playerHealthRestoreCostText", healthCost);
        SetTextRef(so, "fasterGenerationCostText",    fasterGenCost);
        SetTextRef(so, "animalHealthCostText",        animalHealthCost);
        SetTextRef(so, "playerDamagePowerCostText",   playerDmgCost);
        SetTextRef(so, "playerMovePowerCostText",     playerMoveCost);
        SetTextRef(so, "reducedSpawnDelayCostText",   spawnCost);
        SetTextRef(so, "currentResourcesText",        resTxt);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CleanLegacyShopChildren(Transform panel)
    {
        var legacyNames = new HashSet<string>
        {
            "BtnMejorarVida", "BtnMejorarProduccion", "BtnAtaqueAnimales",
            "BtnMejorarVelocidad", "BtnMejorarAtaque", "BtnCerrarTienda",
            "LeftBoxRow1", "LeftBoxRow2", "LeftBoxRow3",
            "RightBoxRow1", "RightBoxRow2", "RightBoxRow3",
            "Icon_CowCostText", "Icon_ChickenCostText", "Icon_PigCostText"
        };
        var toDelete = new List<GameObject>();
        for (var i = 0; i < panel.childCount; i++)
        {
            var ch = panel.GetChild(i);
            if (ch != null && legacyNames.Contains(ch.name))
                toDelete.Add(ch.gameObject);
        }
        foreach (var go in toDelete)
            UnityEngine.Object.DestroyImmediate(go);
    }

    // Etiqueta de coste en la columna izquierda o derecha
    private static Text EnsureCostLabel(Transform panel, string name, float minY, float maxY, bool isLeft)
    {
        float minX = isLeft ? 0.30f : 0.81f;
        float maxX = isLeft ? 0.49f : 0.99f;

        var txt = FindChildComponentByName<Text>(panel, name);
        if (txt == null)
            txt = CreateHudText(panel, name, new Vector2(minX, minY), new Vector2(maxX, maxY), "—");
        else if (txt.transform is RectTransform rt)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        txt.color                = new Color(0.95f, 0.88f, 0.72f, 1f);
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize    = 8;
        txt.resizeTextMaxSize    = 13;
        txt.alignment            = TextAnchor.MiddleLeft;
        return txt;
    }

    // Etiqueta de texto genérica con coordenadas completas
    private static Text EnsureTextLabel(Transform panel, string name,
        float minY, float maxY, float minX, float maxX, string defaultText)
    {
        var txt = FindChildComponentByName<Text>(panel, name);
        if (txt == null)
            txt = CreateHudText(panel, name, new Vector2(minX, minY), new Vector2(maxX, maxY), defaultText);
        else if (txt.transform is RectTransform rt)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        return txt;
    }

    // Icono con coordenadas x explícitas
    private static void EnsureShopIcon(Transform parent, string iconName,
        float minY, float maxY, float minX, float maxX, string spriteAssetPath)
    {
        var icon = FindChildComponentByName<Image>(parent, iconName);
        if (icon == null)
            icon = CreateIconImage(parent, iconName, new Vector2(minX, minY), new Vector2(maxX, maxY), Color.white);
        else if (icon.transform is RectTransform rt)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        AssignIconSprite(icon, spriteAssetPath);
    }

    // Encabezado de sección con coordenadas completas
    private static void EnsureHeaderAt(Transform panel, string name, string value,
        float minY, float maxY, float minX, float maxX, int size, TextAnchor anchor)
    {
        var txt = FindChildComponentByName<Text>(panel, name);
        if (txt == null)
            txt = CreateHudText(panel, name, new Vector2(minX, minY), new Vector2(maxX, maxY), value);
        else if (txt.transform is RectTransform rt)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        txt.text      = value;
        txt.fontSize  = size;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = anchor;
        txt.color     = new Color(0.93f, 0.84f, 0.62f, 1f);
    }

    private static void LayoutShopButtons(Transform panel, ShopUI shopUi)
    {
        var purple = new Color(0.36f, 0.20f, 0.56f, 1f);
        var blue   = new Color(0.20f, 0.26f, 0.56f, 1f);
        var green  = new Color(0.16f, 0.46f, 0.22f, 1f);
        var dark   = new Color(0.22f, 0.18f, 0.42f, 1f);
        var red    = new Color(0.64f, 0.15f, 0.17f, 1f);

        // (nombre, minY, maxY, etiqueta, color, isLeft)
        (string name, float minY, float maxY, string label, Color color, bool isLeft)[] rows =
        {
            ("BuyCow",                      0.765f, 0.828f, "Comprar Vaca",      purple, true),
            ("BuyChicken",                  0.695f, 0.758f, "Comprar Gallina",   purple, true),
            ("BuyPig",                      0.625f, 0.688f, "Comprar Cerdo",     purple, true),
            ("BuyAttackUpgrade",            0.506f, 0.568f, "Mejorar Ataque",    blue,   true),
            ("BuySpeedUpgrade",             0.436f, 0.498f, "Mejorar Velocidad", blue,   true),
            ("BuyPlayerHealthRestore",      0.366f, 0.428f, "Restaurar Vida",    green,  true),
            ("BuyFasterGenerationPowerUp",  0.765f, 0.828f, "Prod. Rapida",      dark,   false),
            ("BuyAnimalHealthPowerUp",      0.695f, 0.758f, "Vida Animal",       dark,   false),
            ("BuyPlayerDamagePowerUp",      0.625f, 0.688f, "Danio Jugador",     dark,   false),
            ("BuyPlayerMovePowerUp",        0.555f, 0.618f, "Mov. Jugador",      dark,   false),
            ("BuyReducedSpawnDelayPowerUp", 0.485f, 0.548f, "Reducir Spawn",     dark,   false),
            ("CloseShop",                   0.030f, 0.120f, "Cerrar Tienda",     red,    true),
        };

        foreach (var (name, minY, maxY, label, color, isLeft) in rows)
        {
            float btnMinX, btnMaxX;
            if (name == "CloseShop") { btnMinX = 0.20f; btnMaxX = 0.80f; }
            else                     { btnMinX = isLeft ? 0.09f  : 0.595f;
                                       btnMaxX = isLeft ? 0.295f : 0.800f; }

            var btn = FindChildComponentByName<Button>(panel, name);
            if (btn == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(panel, false);
                var img = go.GetComponent<Image>();
                img.color = color;
                btn = go.GetComponent<Button>();
                btn.targetGraphic = img;

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                txtGo.transform.SetParent(go.transform, false);
                var txrt = txtGo.GetComponent<RectTransform>();
                txrt.anchorMin = Vector2.zero; txrt.anchorMax = Vector2.one;
                txrt.offsetMin = Vector2.zero; txrt.offsetMax = Vector2.zero;
                var t = txtGo.GetComponent<Text>();
                t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                           ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.alignment = TextAnchor.MiddleCenter;
                t.color     = Color.white;
                t.fontStyle = FontStyle.Bold;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize    = 9;
                t.resizeTextMaxSize    = 16;

                WireButtonToShopUI(btn, shopUi, name);
            }

            // Actualizar apariencia (también en botones ya existentes)
            var btnImg = btn.GetComponent<Image>();
            if (btnImg != null) btnImg.color = color;

            var btnTxt = btn.GetComponentInChildren<Text>(true);
            if (btnTxt != null)
            {
                btnTxt.text      = label;
                btnTxt.color     = Color.white;
                btnTxt.fontStyle = FontStyle.Bold;
                btnTxt.resizeTextForBestFit = true;
                btnTxt.resizeTextMinSize    = 9;
                btnTxt.resizeTextMaxSize    = 16;
            }

            if (btn.transform is RectTransform rt)
            {
                rt.anchorMin = new Vector2(btnMinX, minY);
                rt.anchorMax = new Vector2(btnMaxX, maxY);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }

    private static void WireButtonToShopUI(Button btn, ShopUI shopUi, string buttonName)
    {
        if (btn == null || shopUi == null) return;

        var methodName = buttonName switch
        {
            "BuyCow"                      => "OnBuyCow",
            "BuyChicken"                  => "OnBuyChicken",
            "BuyPig"                      => "OnBuyPig",
            "BuyAttackUpgrade"            => "OnBuyAttackUpgrade",
            "BuySpeedUpgrade"             => "OnBuySpeedUpgrade",
            "BuyPlayerHealthRestore"      => "OnBuyPlayerHealthRestore",
            "BuyFasterGenerationPowerUp"  => "OnBuyFasterGenerationPowerUp",
            "BuyAnimalHealthPowerUp"      => "OnBuyAnimalHealthPowerUp",
            "BuyPlayerDamagePowerUp"      => "OnBuyPlayerDamagePowerUp",
            "BuyPlayerMovePowerUp"        => "OnBuyPlayerMovePowerUp",
            "BuyReducedSpawnDelayPowerUp" => "OnBuyReducedSpawnDelayPowerUp",
            "CloseShop"                   => "Close",
            _ => null
        };
        if (methodName == null) return;

        // Evitar duplicados
        for (var i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
        {
            if (btn.onClick.GetPersistentTarget(i) == (UnityEngine.Object)shopUi &&
                btn.onClick.GetPersistentMethodName(i) == methodName)
                return;
        }

        var method = typeof(ShopUI).GetMethod(methodName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null)
        {
            Debug.LogWarning($"[Scene00Setup] ShopUI.{methodName} no encontrado");
            return;
        }

        var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), shopUi, method);
        UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RESOURCE HUD – barra superior con iconos de leche / huevos / carne
    // ─────────────────────────────────────────────────────────────────────────

    private static void EnsureResourceHud(GameObject uiManagerGo, ResourceUI resourceUi)
    {
        var canvas = FindSceneObjectByName("Canvas");
        if (canvas == null || resourceUi == null) return;

        // Barra superior izquierda (no solapar DayNightHud que está en x: 0.79-0.98)
        var bar = FindSceneObjectByName("ResourceHudBar");
        if (bar == null)
        {
            bar = new GameObject("ResourceHudBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(canvas.transform, false);
        }
        if (bar.transform is RectTransform barRt)
        {
            barRt.anchorMin = new Vector2(0.00f, 0.93f);
            barRt.anchorMax = new Vector2(0.43f, 1.00f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;
        }
        var barImg = bar.GetComponent<Image>();
        if (barImg != null) barImg.color = new Color(0.06f, 0.05f, 0.10f, 0.82f);

        // Celdas compactas: [icono][número] × 3 juntas, sin espacios grandes
        CleanOrphanedHudTexts(canvas.transform);
        var milkIcon = EnsureHudCell(bar.transform, "MilkIcon", "MilkHud",
            0.02f, 0.11f, 0.12f, 0.32f, "Assets/resources/LecheGota.png");
        var eggIcon  = EnsureHudCell(bar.transform, "EggIcon",  "EggHud",
            0.35f, 0.44f, 0.45f, 0.65f, "Assets/resources/HuevoGota.png");
        var meatIcon = EnsureHudCell(bar.transform, "MeatIcon", "MeatHud",
            0.68f, 0.77f, 0.78f, 0.97f, "Assets/resources/CarneGota.png");

        var milkText = FindChildComponentByName<Text>(bar.transform, "MilkHud");
        var eggText  = FindChildComponentByName<Text>(bar.transform, "EggHud");
        var meatText = FindChildComponentByName<Text>(bar.transform, "MeatHud");

        // Asignar a ResourceUI
        var so = new SerializedObject(resourceUi);
        SetFieldRef(so, "milkText",  milkText);
        SetFieldRef(so, "eggText",   eggText);
        SetFieldRef(so, "meatText",  meatText);
        SetFieldRef(so, "milkIcon",  milkIcon);
        SetFieldRef(so, "eggIcon",   eggIcon);
        SetFieldRef(so, "meatIcon",  meatIcon);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(bar);
    }

    // Crea icono + text dentro de la barra de HUD. Devuelve la Image del icono.
    private static Image EnsureHudCell(Transform bar, string iconName, string textName,
        float iconMinX, float iconMaxX, float txtMinX, float txtMaxX, string spritePath)
    {
        // Icono
        var icon = FindChildComponentByName<Image>(bar, iconName);
        if (icon == null)
            icon = CreateIconImage(bar, iconName, new Vector2(iconMinX, 0.10f), new Vector2(iconMaxX, 0.90f), Color.white);
        else if (icon.transform is RectTransform irt)
        {
            irt.anchorMin = new Vector2(iconMinX, 0.10f);
            irt.anchorMax = new Vector2(iconMaxX, 0.90f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
        }
        AssignIconSprite(icon, spritePath);

        // Texto con el número
        var txt = FindChildComponentByName<Text>(bar, textName);
        if (txt == null)
            txt = CreateHudText(bar, textName, new Vector2(txtMinX, 0.05f), new Vector2(txtMaxX, 0.95f), "0");
        else if (txt.transform is RectTransform trt)
        {
            trt.anchorMin = new Vector2(txtMinX, 0.05f);
            trt.anchorMax = new Vector2(txtMaxX, 0.95f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }
        txt.fontSize            = 20;
        txt.fontStyle           = FontStyle.Bold;
        txt.color               = new Color(0.95f, 0.90f, 0.75f, 1f);
        txt.alignment           = TextAnchor.MiddleLeft;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize   = 14;
        txt.resizeTextMaxSize   = 22;

        return icon;
    }

    private static void SetFieldRef(SerializedObject so, string fieldName, UnityEngine.Object value)
    {
        var p = so.FindProperty(fieldName);
        if (p != null) p.objectReferenceValue = value;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PAUSE MENU – panel de pausa al presionar ESC
    // ─────────────────────────────────────────────────────────────────────────

    private static void EnsurePauseMenu()
    {
        var canvas = FindSceneObjectByName("Canvas");
        if (canvas == null) return;

        // Contenedor PauseMenuUI (componente)
        var holder = FindSceneObjectByName("PauseMenuHolder");
        if (holder == null)
        {
            holder = new GameObject("PauseMenuHolder", typeof(RectTransform), typeof(PauseMenuUI));
            holder.transform.SetParent(canvas.transform, false);
            var hrt = holder.GetComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
        }
        var pauseUi = GetOrAdd<PauseMenuUI>(holder);

        // Panel raíz (overlay + caja centrada)
        var panel = FindSceneObjectByName("PauseMenuPanel");
        if (panel == null)
        {
            // Overlay oscuro
            panel = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(holder.transform, false);
        }
        panel.SetActive(false);
        if (panel.transform is RectTransform overlayRt)
        {
            overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero; overlayRt.offsetMax = Vector2.zero;
        }
        var overlayImg = panel.GetComponent<Image>();
        if (overlayImg != null) overlayImg.color = new Color(0f, 0f, 0f, 0.72f);

        // Caja del menú centrada
        var box = FindSceneObjectByName("PauseMenuBox");
        if (box == null)
        {
            box = new GameObject("PauseMenuBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            box.transform.SetParent(panel.transform, false);
        }
        if (box.transform is RectTransform boxRt)
        {
            boxRt.anchorMin = new Vector2(0.30f, 0.25f);
            boxRt.anchorMax = new Vector2(0.70f, 0.78f);
            boxRt.offsetMin = Vector2.zero; boxRt.offsetMax = Vector2.zero;
        }
        var boxImg = box.GetComponent<Image>();
        if (boxImg != null) boxImg.color = new Color(0.07f, 0.06f, 0.12f, 0.97f);

        // Título "PAUSA"
        EnsureHeaderAt(box.transform, "PauseTitle", "PAUSA",
            0.82f, 0.97f, 0.05f, 0.95f, 26, TextAnchor.MiddleCenter);

        // Botones: Continuar / Reiniciar / Menú
        (string name, float minY, float maxY, string label, Color color)[] btns =
        {
            ("BtnResume",  0.57f, 0.74f, "Continuar",       new Color(0.16f, 0.46f, 0.22f, 1f)),
            ("BtnRestart", 0.35f, 0.52f, "Reiniciar",       new Color(0.20f, 0.26f, 0.56f, 1f)),
            ("BtnMenu",    0.10f, 0.27f, "Menu Principal",  new Color(0.64f, 0.15f, 0.17f, 1f)),
        };

        foreach (var (bName, minY, maxY, label, color) in btns)
        {
            var btn = FindChildComponentByName<Button>(box.transform, bName);
            if (btn == null)
            {
                var bgo = new GameObject(bName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                bgo.transform.SetParent(box.transform, false);
                var bimg = bgo.GetComponent<Image>();
                bimg.color = color;
                btn = bgo.GetComponent<Button>();
                btn.targetGraphic = bimg;

                var tgo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                tgo.transform.SetParent(bgo.transform, false);
                var trt = tgo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
                var t = tgo.GetComponent<Text>();
                t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                           ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.alignment = TextAnchor.MiddleCenter;
                t.color     = Color.white;
                t.fontStyle = FontStyle.Bold;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize    = 12;
                t.resizeTextMaxSize    = 22;

                WirePauseButton(btn, pauseUi, bName);
            }

            var bImg = btn.GetComponent<Image>();
            if (bImg != null) bImg.color = color;
            var bTxt = btn.GetComponentInChildren<Text>(true);
            if (bTxt != null) { bTxt.text = label; bTxt.color = Color.white; bTxt.fontStyle = FontStyle.Bold; }

            if (btn.transform is RectTransform brt)
            {
                brt.anchorMin = new Vector2(0.08f, minY);
                brt.anchorMax = new Vector2(0.92f, maxY);
                brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            }
        }

        // Wire panelRoot → PauseMenuUI
        var soP = new SerializedObject(pauseUi);
        var panelProp = soP.FindProperty("panelRoot");
        if (panelProp != null) panelProp.objectReferenceValue = panel;
        soP.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(holder);
    }

    private static void WirePauseButton(Button btn, PauseMenuUI pauseUi, string buttonName)
    {
        if (btn == null || pauseUi == null) return;
        var methodName = buttonName switch
        {
            "BtnResume"  => "Resume",
            "BtnRestart" => "Restart",
            "BtnMenu"    => "GoToMenu",
            _ => null
        };
        if (methodName == null) return;

        for (var i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
        {
            if (btn.onClick.GetPersistentTarget(i) == (UnityEngine.Object)pauseUi &&
                btn.onClick.GetPersistentMethodName(i) == methodName)
                return;
        }

        var method = typeof(PauseMenuUI).GetMethod(methodName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null) return;

        var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), pauseUi, method);
        UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ENEMY RESOURCE DROP – agrega EnemyResourceDrop a prefabs de enemigos
    // ─────────────────────────────────────────────────────────────────────────
    // BLOOD EFFECT – partículas de sangre en enemigos y animales
    // ─────────────────────────────────────────────────────────────────────────

    private static void AddBloodEffect(EnemyBase enemyPrefab)
    {
        if (enemyPrefab == null) return;
        if (enemyPrefab.GetComponent<BloodHitEffect>() == null)
            enemyPrefab.gameObject.AddComponent<BloodHitEffect>();

        const string rosePath = "Assets/prefabs_02/enemies/MvpRangedPlant.prefab";
        var rose = AssetDatabase.LoadAssetAtPath<GameObject>(rosePath);
        if (rose != null && rose.GetComponent<BloodHitEffect>() == null)
            rose.AddComponent<BloodHitEffect>();

        AssetDatabase.SaveAssets();
    }

    private static void AddBloodEffectToAnimalPrefabs()
    {
        var animalPrefabPaths = new[]
        {
            "Assets/prefabs_02/animals/MvpFarmAnimal_Chicken.prefab",
            "Assets/prefabs_02/animals/MvpFarmAnimal_Cow.prefab",
            "Assets/prefabs_02/animals/MvpFarmAnimal_Pig.prefab",
        };
        foreach (var path in animalPrefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<BloodHitEffect>() == null)
                prefab.AddComponent<BloodHitEffect>();
        }
        AssetDatabase.SaveAssets();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CLEANUP – eliminar nodos HUD obsoletos de la escena
    // ─────────────────────────────────────────────────────────────────────────

    private static void CleanupObsoleteSceneNodes()
    {
        var obsolete = new HashSet<string>
        {
            "GoldText", "WoodText", "StoneText",
            "DayNightText", "HealthText", "ScoreText",
            "MilkText", "EggText", "MeatText",
        };

        var canvas = FindSceneObjectByName("Canvas");
        if (canvas == null) return;

        var resourceBar = FindSceneObjectByName("ResourceHudBar");
        var toDelete = new List<GameObject>();

        // Buscar en Canvas y UIManager
        foreach (var searchRoot in new[] { canvas, FindSceneObjectByName("UIManager") })
        {
            if (searchRoot == null) continue;
            for (var i = 0; i < searchRoot.transform.childCount; i++)
            {
                var ch = searchRoot.transform.GetChild(i);
                if (ch == null) continue;
                // Texto huérfano de recursos
                if ((ch.name == "MilkHud" || ch.name == "EggHud" || ch.name == "MeatHud")
                    && (resourceBar == null || ch.gameObject != resourceBar))
                    toDelete.Add(ch.gameObject);
                // Otros nodos obsoletos
                if (obsolete.Contains(ch.name))
                    toDelete.Add(ch.gameObject);
            }
        }

        foreach (var go in toDelete)
        {
            Debug.Log($"[Scene00Setup] Eliminando nodo obsoleto: {go.name}");
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static void AddEnemyResourceDrop(EnemyBase enemyPrefab)
    {
        if (enemyPrefab == null) return;

        if (enemyPrefab.GetComponent<EnemyResourceDrop>() == null)
            enemyPrefab.gameObject.AddComponent<EnemyResourceDrop>();

        // También añadir a RangedPlant si existe
        const string rosePath = "Assets/prefabs_02/enemies/MvpRangedPlant.prefab";
        var rose = AssetDatabase.LoadAssetAtPath<GameObject>(rosePath);
        if (rose != null && rose.GetComponent<EnemyResourceDrop>() == null)
            rose.AddComponent<EnemyResourceDrop>();

        AssetDatabase.SaveAssets();
        Debug.Log("[Scene00Setup] EnemyResourceDrop added to enemy prefabs.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS GENERALES
    // ─────────────────────────────────────────────────────────────────────────

    private static void EnsureSectionHeader(Transform panel, string name, string value,
        float minY, float maxY, int size, TextAnchor anchor)
    {
        var txt = FindChildComponentByName<Text>(panel, name);
        if (txt == null)
            txt = CreateHudText(panel, name, new Vector2(0.04f, minY), new Vector2(0.96f, maxY), value);
        else if (txt.transform is RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.04f, minY);
            rt.anchorMax = new Vector2(0.96f, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        txt.text      = value;
        txt.fontSize  = size;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = anchor;
        txt.color     = new Color(0.93f, 0.84f, 0.62f, 1f);
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
        rect.anchorMin = new Vector2(0.04f, 0.03f);
        rect.anchorMax = new Vector2(0.96f, 0.97f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.07f, 0.06f, 0.11f, 0.96f);

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
            root = new GameObject("DayNightHud", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
        }
        // Always update anchors + background
        if (root.transform is RectTransform rootRt)
        {
            rootRt.anchorMin = new Vector2(0.78f, 0.87f);
            rootRt.anchorMax = new Vector2(0.99f, 1.00f);
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
        }
        var rootImg = root.GetComponent<Image>() ?? root.AddComponent<Image>();
        rootImg.color = new Color(0.06f, 0.05f, 0.10f, 0.82f);

        void SetChildAnchor(Component c, Vector2 mn, Vector2 mx)
        {
            if (c?.transform is RectTransform r2)
            {
                r2.anchorMin = mn; r2.anchorMax = mx;
                r2.offsetMin = Vector2.zero; r2.offsetMax = Vector2.zero;
            }
        }

        var sun      = FindChildComponentByName<Image>(root.transform, "SunIcon")   ?? CreateIconImage(root.transform, "SunIcon",   new Vector2(0.02f, 0.08f), new Vector2(0.23f, 0.92f), new Color(1f, 0.84f, 0.2f, 1f));
        var moon     = FindChildComponentByName<Image>(root.transform, "MoonIcon")  ?? CreateIconImage(root.transform, "MoonIcon",  new Vector2(0.02f, 0.08f), new Vector2(0.23f, 0.92f), new Color(0.67f, 0.75f, 1f, 1f));
        var clockBg  = FindChildComponentByName<Image>(root.transform, "ClockBg")   ?? CreateIconImage(root.transform, "ClockBg",   new Vector2(0.25f, 0.55f), new Vector2(0.98f, 0.95f), new Color(0f, 0f, 0f, 0.35f));
        var fill     = FindChildComponentByName<Image>(root.transform, "ClockFill") ?? CreateIconImage(root.transform, "ClockFill", new Vector2(0.25f, 0.55f), new Vector2(0.98f, 0.95f), new Color(0.35f, 0.95f, 0.9f, 0.85f));
        var clockTxt = FindChildComponentByName<Text>(root.transform, "ClockText")  ?? CreateHudText(root.transform, "ClockText", new Vector2(0.25f, 0.04f), new Vector2(0.98f, 0.50f), "Día 120s");
        var _        = FindChildComponentByName<RectTransform>(root.transform, "ClockHand") ?? CreateClockHand(root.transform);

        // Always reposition all children
        SetChildAnchor(sun,      new Vector2(0.02f, 0.08f), new Vector2(0.23f, 0.92f));
        SetChildAnchor(moon,     new Vector2(0.02f, 0.08f), new Vector2(0.23f, 0.92f));
        SetChildAnchor(clockBg,  new Vector2(0.25f, 0.55f), new Vector2(0.98f, 0.95f));
        SetChildAnchor(fill,     new Vector2(0.25f, 0.55f), new Vector2(0.98f, 0.95f));
        SetChildAnchor(clockTxt, new Vector2(0.25f, 0.04f), new Vector2(0.98f, 0.50f));

        clockTxt.alignment           = TextAnchor.MiddleCenter;
        clockTxt.fontStyle           = FontStyle.Bold;
        clockTxt.color               = new Color(0.95f, 0.90f, 0.75f, 1f);
        clockTxt.resizeTextForBestFit = true;
        clockTxt.resizeTextMinSize   = 10;
        clockTxt.resizeTextMaxSize   = 20;

        fill.type          = Image.Type.Filled;
        fill.fillMethod    = Image.FillMethod.Radial360;
        fill.fillClockwise = true;
        fill.fillOrigin    = 2;
        fill.fillAmount    = 0f;
        moon.enabled       = false;
        sun.enabled        = true;
        AssignIconSprite(sun,  "Assets/resources/Sol.png");
        AssignIconSprite(moon, "Assets/resources/Luna.png");

        return root;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HEALTH BAR HUD – barra de vida del jugador debajo del resource bar
    // ─────────────────────────────────────────────────────────────────────────

    private static HealthBar EnsureHealthBarHud()
    {
        var canvas = FindSceneObjectByName("Canvas");
        if (canvas == null) return null;

        var container = FindSceneObjectByName("HealthBarHud");
        if (container == null)
        {
            container = new GameObject("HealthBarHud", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            container.transform.SetParent(canvas.transform, false);
        }
        if (container.transform is RectTransform cRt)
        {
            cRt.anchorMin = new Vector2(0.00f, 0.87f);
            cRt.anchorMax = new Vector2(0.43f, 0.93f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
        }
        var cImg = container.GetComponent<Image>();
        if (cImg != null) cImg.color = new Color(0.06f, 0.05f, 0.10f, 0.82f);

        var healthBar = GetOrAdd<HealthBar>(container);

        // Icono de corazón (cuadrado rojo — sin sprite dedicado)
        var heart = FindChildComponentByName<Image>(container.transform, "HeartIcon");
        if (heart == null)
            heart = CreateIconImage(container.transform, "HeartIcon",
                new Vector2(0.02f, 0.12f), new Vector2(0.13f, 0.88f), new Color(0.88f, 0.15f, 0.15f, 1f));
        if (heart.transform is RectTransform hRt)
        {
            hRt.anchorMin = new Vector2(0.02f, 0.12f);
            hRt.anchorMax = new Vector2(0.13f, 0.88f);
            hRt.offsetMin = Vector2.zero; hRt.offsetMax = Vector2.zero;
        }

        // Slider root
        var sliderGo = FindSceneObjectByName("HealthSlider");
        if (sliderGo == null)
        {
            sliderGo = new GameObject("HealthSlider", typeof(RectTransform));
            sliderGo.transform.SetParent(container.transform, false);
        }
        else if (sliderGo.transform.parent != container.transform)
            sliderGo.transform.SetParent(container.transform, false);

        if (sliderGo.transform is RectTransform sRt)
        {
            sRt.anchorMin = new Vector2(0.15f, 0.15f);
            sRt.anchorMax = new Vector2(0.97f, 0.85f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;
        }

        var slider = GetOrAdd<Slider>(sliderGo);
        slider.direction    = Slider.Direction.LeftToRight;
        slider.minValue     = 0f;
        slider.maxValue     = 10f;
        slider.value        = 10f;
        slider.interactable = false;
        slider.wholeNumbers = true;

        // Fondo oscuro
        var bgImg = FindChildComponentByName<Image>(sliderGo.transform, "HealthBg");
        if (bgImg == null)
        {
            var bg = new GameObject("HealthBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            bgImg = bg.GetComponent<Image>();
        }
        bgImg.color = new Color(0.28f, 0.05f, 0.05f, 1f);
        if (bgImg.transform is RectTransform bgRt)
        {
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        }

        // Fill Area
        var fillArea = FindChildComponentByName<RectTransform>(sliderGo.transform, "FillArea");
        if (fillArea == null)
        {
            var fa = new GameObject("FillArea", typeof(RectTransform));
            fa.transform.SetParent(sliderGo.transform, false);
            fillArea = fa.GetComponent<RectTransform>();
        }
        fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = Vector2.zero; fillArea.offsetMax = Vector2.zero;

        // Fill Image
        var fillImg = FindChildComponentByName<Image>(fillArea, "Fill");
        if (fillImg == null)
        {
            var fi = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fi.transform.SetParent(fillArea, false);
            fillImg = fi.GetComponent<Image>();
        }
        fillImg.color = new Color(0.85f, 0.12f, 0.12f, 1f);
        if (fillImg.transform is RectTransform fiRt)
        {
            fiRt.anchorMin = Vector2.zero; fiRt.anchorMax = Vector2.one;
            fiRt.offsetMin = Vector2.zero; fiRt.offsetMax = Vector2.zero;
        }

        // Wire Slider.fillRect y HealthBar.slider
        var sliderSo = new SerializedObject(slider);
        sliderSo.FindProperty("m_FillRect").objectReferenceValue = fillImg.transform as RectTransform;
        sliderSo.ApplyModifiedPropertiesWithoutUndo();

        var hbSo = new SerializedObject(healthBar);
        hbSo.FindProperty("slider").objectReferenceValue = slider;
        hbSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(container);
        return healthBar;
    }

    // Elimina Text huérfanos (MilkHud/EggHud/MeatHud) sueltos en Canvas
    private static void CleanOrphanedHudTexts(Transform canvasTransform)
    {
        var bar = FindSceneObjectByName("ResourceHudBar");
        var toDelete = new List<GameObject>();
        for (var i = 0; i < canvasTransform.childCount; i++)
        {
            var ch = canvasTransform.GetChild(i);
            if (ch == null) continue;
            if ((ch.name == "MilkHud" || ch.name == "EggHud" || ch.name == "MeatHud")
                && (bar == null || ch.gameObject != bar))
                toDelete.Add(ch.gameObject);
        }
        foreach (var go in toDelete)
            UnityEngine.Object.DestroyImmediate(go);
    }

    private static void EnsureResourceHudIcons()
    {
        var hudBar   = FindSceneObjectByName("ResourceHudBar");
        var milkText = FindSceneObjectByName("MilkHud");
        var eggText  = FindSceneObjectByName("EggHud");
        var meatText = FindSceneObjectByName("MeatHud");

        if (hudBar != null)
        {
            EnsureHudIconAtBar(hudBar.transform, "MilkIcon", new Vector2(0.006f, 0.15f), new Vector2(0.05f, 0.86f), "Assets/resources/LecheGota.png");
            EnsureHudIconAtBar(hudBar.transform, "EggIcon",  new Vector2(0.336f, 0.15f), new Vector2(0.38f, 0.86f), "Assets/resources/HuevoGota.png");
            EnsureHudIconAtBar(hudBar.transform, "MeatIcon", new Vector2(0.666f, 0.15f), new Vector2(0.71f, 0.86f), "Assets/resources/CarneGota.png");
        }

        if (milkText != null) EnsureIconNearText(milkText.transform, "MilkIconFallback", "Assets/resources/LecheGota.png");
        if (eggText  != null) EnsureIconNearText(eggText.transform,  "EggIconFallback",  "Assets/resources/HuevoGota.png");
        if (meatText != null) EnsureIconNearText(meatText.transform, "MeatIconFallback", "Assets/resources/CarneGota.png");
    }

    private static void EnsureIconNearText(Transform textTransform, string iconName, string spriteAssetPath)
    {
        var parent = textTransform.parent;
        if (parent == null) return;
        if (textTransform is not RectTransform textRt) return;

        var icon     = FindChildComponentByName<Image>(parent, iconName);
        var iconMinX = Mathf.Clamp01(textRt.anchorMin.x - 0.07f);
        var iconMaxX = Mathf.Clamp01(textRt.anchorMin.x - 0.015f);
        var iconMinY = textRt.anchorMin.y + 0.06f;
        var iconMaxY = textRt.anchorMax.y - 0.06f;
        if (icon == null)
            icon = CreateIconImage(parent, iconName, new Vector2(iconMinX, iconMinY), new Vector2(iconMaxX, iconMaxY), Color.white);
        else if (icon.transform is RectTransform irt)
        {
            irt.anchorMin = new Vector2(iconMinX, iconMinY);
            irt.anchorMax = new Vector2(iconMaxX, iconMaxY);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
        }

        var off = textRt.offsetMin;
        off.x = Mathf.Max(off.x, 34f);
        textRt.offsetMin = off;

        AssignIconSprite(icon, spriteAssetPath);
    }

    private static void EnsureHudIconAtBar(Transform bar, string iconName, Vector2 min, Vector2 max, string spriteAssetPath)
    {
        var icon = FindChildComponentByName<Image>(bar, iconName);
        if (icon == null)
            icon = CreateIconImage(bar, iconName, min, max, Color.white);
        else if (icon.transform is RectTransform irt)
        {
            irt.anchorMin = min;
            irt.anchorMax = max;
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
        }
        AssignIconSprite(icon, spriteAssetPath);
    }

    private static Text EnsureWaveCounterHud()
    {
        var canvas = FindSceneObjectByName("Canvas");
        if (canvas == null)
            return null;

        var txt = FindSceneObjectByName("WaveCounterText");
        if (txt == null)
        {
            var go = new GameObject("WaveCounterText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            txt = go;
        }

        var rt = txt.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.78f, 0.80f);
        rt.anchorMax = new Vector2(0.98f, 0.86f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var t = txt.GetComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize  = 20;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleRight;
        t.color     = new Color(1f, 0.92f, 0.45f, 1f);
        if (string.IsNullOrWhiteSpace(t.text))
            t.text = "Oleada 1";
        return t;
    }

    private static void AssignIconSprite(Image image, string spriteAssetPath)
    {
        if (image == null) return;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
        if (sprite == null) return;
        image.sprite        = sprite;
        image.preserveAspect = true;
        image.color         = Color.white;
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
        t.fontSize  = 16;
        t.alignment = TextAnchor.MiddleLeft;
        t.color     = Color.white;
        t.text      = value;
        return t;
    }

    private static RectTransform CreateClockHand(Transform parent)
    {
        var go = new GameObject("ClockHand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin      = new Vector2(0.385f, 0.5f);
        rt.anchorMax      = new Vector2(0.385f, 0.5f);
        rt.sizeDelta      = new Vector2(4f, 26f);
        rt.anchoredPosition = Vector2.zero;
        rt.pivot          = new Vector2(0.5f, 0f);
        go.GetComponent<Image>().color = Color.white;
        return rt;
    }

    private static T FindChildComponentByName<T>(Transform parent, string childName) where T : Component
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var ch = parent.GetChild(i);
            if (ch.name != childName) continue;
            if (ch.TryGetComponent<T>(out var c)) return c;
        }
        return null;
    }

    private static UnityEngine.Object FindGlobalLight2D()
    {
        var lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (lightType == null) return null;

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

        var walk  = LoadSpritesFromSheet("Assets/animations_04/enemies/CalabazaCamina.png");
        var death = LoadSpritesFromSheet("Assets/animations_04/enemies/CalabazaMuere.png");
        if (walk.Count == 0)
        {
            Debug.LogWarning("[Scene00Setup] Could not create pumpkin prefab (missing CalabazaCamina sprites).");
            return null;
        }

        var root = new GameObject("MvpPumpkinEnemy");
        root.layer = LayerMask.NameToLayer("Enemy");
        root.tag   = "Enemy";
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite       = walk[0];
        sr.sortingOrder = 2;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(Mathf.Max(0.8f, walk[0].bounds.size.x * 0.5f),
                               Mathf.Max(0.8f, walk[0].bounds.size.y * 0.48f));
        root.AddComponent<Animator>();
        var enemy      = root.AddComponent<PumpkinEnemy>();
        var ai         = root.AddComponent<EnemyAI>();
        var spriteAnim = root.AddComponent<EnemySpriteAnimator>();

        var enemySo = new SerializedObject(enemy);
        enemySo.FindProperty("damage").intValue = 2;
        enemySo.FindProperty("attackCooldown").floatValue = 1.05f;
        enemySo.ApplyModifiedPropertiesWithoutUndo();

        var aiSo = new SerializedObject(ai);
        aiSo.FindProperty("attackRange").floatValue = 1.1f;
        aiSo.FindProperty("moveSpeed").floatValue   = 2.4f;
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
        if (rose == null) return;

        var so = new SerializedObject(rose);
        var damageProp  = so.FindProperty("damage");
        if (damageProp  != null) damageProp.intValue = 1;
        var projOverride = so.FindProperty("projectileDamageOverride");
        if (projOverride != null) projOverride.intValue = 1;
        var minInterval  = so.FindProperty("minProjectileAttackInterval");
        if (minInterval  != null) minInterval.floatValue = 1.25f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<Sprite> LoadSpritesFromSheet(string path)
    {
        var outList = new List<Sprite>();
        var assets  = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
            if (a is Sprite s) outList.Add(s);
        outList.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return outList;
    }

    private static void FillEnemySpriteAnimator(EnemySpriteAnimator target, SpriteRenderer sr,
        List<Sprite> walk, List<Sprite> death)
    {
        var so = new SerializedObject(target);
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;

        void Fill(string prop, List<Sprite> src)
        {
            var arr = so.FindProperty(prop);
            arr.arraySize = src.Count;
            for (var i = 0; i < src.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = src[i];
        }

        Fill("idleFrames",   walk);
        Fill("walkFrames",   walk);
        Fill("attackFrames", walk);
        Fill("deathFrames",  death);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
