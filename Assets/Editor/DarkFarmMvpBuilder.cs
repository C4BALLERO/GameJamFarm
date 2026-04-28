#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// One-click generator for MVP prefabs and the three gameplay/menu/test scenes. Run from the menu bar after pulling the repo.
/// </summary>
public static class DarkFarmMvpBuilder
{
    private const string PrefabRoot = "Assets/prefabs_02";
    private const string SceneDir = "Assets/scene_00";

    private const string AnimCtrlPlayer = "Assets/animations_05/Player2D.controller";
    private const string AnimCtrlFarmAnimal = "Assets/animations_05/FarmAnimal2D.controller";
    private const string AnimCtrlEnemy = "Assets/animations_05/Enemy2D.controller";

    // Sprite sheets in Assets/animations_04 (first frame: {fileName}_0)
    private const string AnimPlayerWalk = "Assets/animations_04/player/granjeroCamina.png";
    private const string AnimPlayerAttack = "Assets/animations_04/player/granjeroAtaca.png";
    private const string AnimPlayerDeath = "Assets/animations_04/player/granjeroMuere.png";
    private const string AnimCowWalk = "Assets/animations_04/animals/VacaCamina.png";
    private const string AnimChickenWalk = "Assets/animations_04/animals/gallinaCamina.png";
    private const string AnimPigWalk = "Assets/animations_04/animals/cerdoCamina.png";
    private const string AnimEnemyBodyMelee = "Assets/animations_04/enemies/CaminaMaiz.png";
    private const string AnimEnemyBodyRanged = "Assets/animations_04/enemies/CaminaRosa.png";
    private const string AnimEnemyAttackRanged = "Assets/animations_04/enemies/RosaAtaca.png";
    private const string AnimEnemyDeathMelee = "Assets/animations_04/enemies/MuerteMaiz.png";
    private const string AnimEnemyDeathRanged = "Assets/animations_04/enemies/MuerteRosa.png";
    private const string AnimCowDeath = "Assets/animations_04/animals/VacaMuere.png";
    private const string AnimChickenDeath = "Assets/animations_04/animals/gallinaMuere.png";
    private const string AnimPigDeath = "Assets/animations_04/animals/CerdoMuere.png";
    private const string AnimProjectile = "Assets/animations_04/enemies/CalabazaCamina.png";

    /// <summary>Regenera prefabs y assets. No modifica los .unity en scene_00 (no borra Tilemaps).</summary>
    [MenuItem("Dark Farm/Build MVP Prefabs & Assets", priority = 0)]
    public static void BuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[DarkFarmMvpBuilder] Exit Play Mode before running this menu item.");
            return;
        }

        BuildMvpPrefabsCore();

        ApplyBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[DarkFarmMvpBuilder] Prefabs y controladores listos. Las escenas en scene_00/ no se tocaron (mapa / tiles intactos). " +
            "Para regenerar solo el layout procedural desde código, usa el menú destructivo.");
    }

    /// <summary>
    /// Sobrescribe scene_00_main, scene_01_menu y scene_02_test con el generador procedural (borra Tilemaps y objetos hechos a mano en esas escenas).
    /// </summary>
    [MenuItem("Dark Farm/Reconstruir escenas procedurales (DESTRUYE mapa pintado)", priority = 50)]
    public static void RebuildProceduralScenesDestructive()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[DarkFarmMvpBuilder] Exit Play Mode before running this menu item.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Sobrescribir escenas",
                "Se regenerarán desde código scene_00_main, scene_01_menu y scene_02_test. " +
                "Se pierden Tilemaps, capas pintadas y cualquier objeto que no venga del script del builder.\n\n¿Continuar?",
                "Sobrescribir",
                "Cancelar"))
            return;

        var (sprGround, playerPath, plantPath, rangedPath, cowPath, chickenPath, pigPath) = BuildMvpPrefabsCore();

        NewEmptyScene();
        BuildMainScene(
            sprGround,
            playerPath,
            plantPath,
            rangedPath,
            cowPath,
            chickenPath,
            pigPath);

        EditorSceneManager.SaveScene(
            EditorSceneManager.GetActiveScene(),
            $"{SceneDir}/scene_00_main.unity");

        NewEmptyScene();
        BuildMenuScene();
        EditorSceneManager.SaveScene(
            EditorSceneManager.GetActiveScene(),
            $"{SceneDir}/scene_01_menu.unity");

        NewEmptyScene();
        BuildTestScene(sprGround);
        EditorSceneManager.SaveScene(
            EditorSceneManager.GetActiveScene(),
            $"{SceneDir}/scene_02_test.unity");

        ApplyBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DarkFarmMvpBuilder] Escenas procedurales regeneradas (mapa anterior sobrescrito).");
    }

    private static (
        Sprite sprGround,
        string playerPath,
        string plantPath,
        string rangedPath,
        string cowPath,
        string chickenPath,
        string pigPath) BuildMvpPrefabsCore()
    {
        EnsureFolder(PrefabRoot + "/player");
        EnsureFolder(PrefabRoot + "/enemies");
        EnsureFolder(PrefabRoot + "/animals");
        EnsureFolder(PrefabRoot + "/environment");
        EnsureFolder(PrefabRoot + "/ui");
        EnsureFolder(SceneDir);
        EnsureFolder("Assets/sprites_03/tiles");
        EnsureFolder("Assets/animations_05");
        Animator2DControllersGenerator.GenerateAll();

        var sprPlayer = LoadSpriteZeroFrame(AnimPlayerWalk) ??
                        WriteSpritePng("Assets/sprites_03/player/_mvp_player.png", new Color32(220, 220, 235, 255));
        var playerWalkFrames = LoadSpriteFrames(AnimPlayerWalk);
        var playerAttackFrames = LoadSpriteFrames(AnimPlayerAttack);
        var playerDeathFrames = LoadSpriteFrames(AnimPlayerDeath);
        var sprPlant = LoadSpriteZeroFrame(AnimEnemyBodyMelee) ??
                       WriteSpritePng("Assets/sprites_03/enemies/_mvp_plant.png", new Color32(55, 110, 50, 255));
        var sprRangedBody = LoadSpriteZeroFrame(AnimEnemyBodyRanged) ?? sprPlant;
        var enemyMeleeWalkFrames = LoadSpriteFrames(AnimEnemyBodyMelee);
        var enemyMeleeAttackFrames = LoadSpriteFrames(AnimEnemyBodyMelee);
        var enemyMeleeDeathFrames = LoadSpriteFrames(AnimEnemyDeathMelee);
        var enemyRangedWalkFrames = LoadSpriteFrames(AnimEnemyBodyRanged);
        var enemyRangedAttackFrames = LoadSpriteFrames(AnimEnemyAttackRanged);
        if (enemyRangedAttackFrames == null || enemyRangedAttackFrames.Length == 0)
            enemyRangedAttackFrames = enemyRangedWalkFrames;
        var enemyRangedDeathFrames = LoadSpriteFrames(AnimEnemyDeathRanged);
        var sprProj = LoadSpriteZeroFrame(AnimProjectile) ??
                      WriteSpritePng("Assets/sprites_03/enemies/_mvp_seed.png", new Color32(210, 230, 90, 255));
        var sprCow = LoadSpriteZeroFrame(AnimCowWalk) ??
                     WriteSpritePng("Assets/sprites_03/animals/_mvp_animal.png", new Color32(185, 140, 100, 255));
        var sprChicken = LoadSpriteZeroFrame(AnimChickenWalk) ?? sprCow;
        var sprPig = LoadSpriteZeroFrame(AnimPigWalk) ?? sprCow;
        var cowWalkFrames = LoadSpriteFrames(AnimCowWalk);
        var cowDeathFrames = LoadSpriteFrames(AnimCowDeath);
        var chickenWalkFrames = LoadSpriteFrames(AnimChickenWalk);
        var chickenDeathFrames = LoadSpriteFrames(AnimChickenDeath);
        var pigWalkFrames = LoadSpriteFrames(AnimPigWalk);
        var pigDeathFrames = LoadSpriteFrames(AnimPigDeath);
        var sprGround = WriteSpritePng("Assets/sprites_03/tiles/_mvp_ground.png", new Color32(32, 26, 48, 255));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var projectilePath = $"{PrefabRoot}/enemies/MvpProjectile.prefab";
        var plantPath = $"{PrefabRoot}/enemies/MvpPlantEnemy.prefab";
        var rangedPath = $"{PrefabRoot}/enemies/MvpRangedPlant.prefab";
        var playerPath = $"{PrefabRoot}/player/MvpPlayer.prefab";
        var cowPath = $"{PrefabRoot}/animals/MvpFarmAnimal_Cow.prefab";
        var chickenPath = $"{PrefabRoot}/animals/MvpFarmAnimal_Chicken.prefab";
        var pigPath = $"{PrefabRoot}/animals/MvpFarmAnimal_Pig.prefab";

        BuildProjectilePrefab(projectilePath, sprProj);
        BuildPlantEnemyPrefab(plantPath, sprPlant, enemyMeleeWalkFrames, enemyMeleeAttackFrames, enemyMeleeDeathFrames);
        BuildRangedPlantPrefab(rangedPath, sprRangedBody, projectilePath, enemyRangedWalkFrames, enemyRangedAttackFrames, enemyRangedDeathFrames);
        BuildPlayerPrefab(playerPath, sprPlayer, playerWalkFrames, playerAttackFrames, playerDeathFrames);
        BuildAnimalPrefab(cowPath, sprCow, FarmAnimalKind.Cow, ResourceType.Milk, 5.5f, 1, 26, cowWalkFrames, cowDeathFrames);
        BuildAnimalPrefab(chickenPath, sprChicken, FarmAnimalKind.Chicken, ResourceType.Egg, 4.25f, 1, 17, chickenWalkFrames, chickenDeathFrames);
        BuildAnimalPrefab(pigPath, sprPig, FarmAnimalKind.Pig, ResourceType.Meat, 3.75f, 1, 22, pigWalkFrames, pigDeathFrames);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return (sprGround, playerPath, plantPath, rangedPath, cowPath, chickenPath, pigPath);
    }

    /// <summary>Clears the default new scene so we start from an empty hierarchy (Unity 6000 has no NewSceneSetup.Empty).</summary>
    private static void NewEmptyScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            Object.DestroyImmediate(go);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            AssetDatabase.CreateFolder(parent, name);
    }

    /// <summary>Loads the first sliced sprite ({basename}_0) from a multi-sprite texture asset.</summary>
    private static Sprite LoadSpriteZeroFrame(string textureAssetPath)
    {
        if (string.IsNullOrEmpty(textureAssetPath))
            return null;

        var baseName = Path.GetFileNameWithoutExtension(textureAssetPath);
        var want = baseName + "_0";
        Sprite first = null;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(textureAssetPath))
        {
            if (!(o is Sprite s)) continue;
            if (first == null) first = s;
            if (s.name == want) return s;
        }

        if (first != null)
            Debug.LogWarning($"[DarkFarmMvpBuilder] Using first sprite '{first.name}' (expected '{want}') in {textureAssetPath}");
        return first;
    }

    private static Sprite[] LoadSpriteFrames(string textureAssetPath)
    {
        var loaded = AssetDatabase.LoadAllAssetsAtPath(textureAssetPath);
        var sprites = new System.Collections.Generic.List<Sprite>();
        foreach (var o in loaded)
        {
            if (o is Sprite s)
                sprites.Add(s);
        }
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }

    private static void FitBoxColliderToSprite(SpriteRenderer sr, BoxCollider2D box, float widthFactor = 0.45f, float heightFactor = 0.42f)
    {
        if (sr == null || box == null || sr.sprite == null) return;
        var b = sr.sprite.bounds;
        box.offset = b.center;
        box.size = new Vector2(b.size.x * widthFactor, b.size.y * heightFactor);
    }

    private static Sprite WriteSpritePng(string assetPath, Color32 fill)
    {
        var dir = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color32[32 * 32];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = fill;
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        ti.textureType = TextureImporterType.Sprite;
        ti.spritePixelsPerUnit = 32;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void BuildProjectilePrefab(string path, Sprite sprite)
    {
        var root = new GameObject("MvpProjectile");
        root.layer = LayerMask.NameToLayer("Default");
        root.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 5;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f;
        root.AddComponent<Projectile>();
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void BuildPlantEnemyPrefab(string path, Sprite sprite, Sprite[] walkFrames, Sprite[] attackFrames, Sprite[] deathFrames)
    {
        var root = new GameObject("MvpPlantEnemy");
        root.tag = "Enemy";
        root.layer = LayerMask.NameToLayer("Enemy");
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.85f, 1f, 0.85f);
        sr.sortingOrder = 2;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var box = root.AddComponent<BoxCollider2D>();
        FitBoxColliderToSprite(sr, box, 0.5f, 0.48f);
        root.AddComponent<Animator>();
        root.AddComponent<PlantEnemy>();
        root.AddComponent<EnemyAI>();

        var visual = root.AddComponent<EnemySpriteAnimator>();
        var vs = new SerializedObject(visual);
        vs.FindProperty("spriteRenderer").objectReferenceValue = sr;
        vs.FindProperty("walkFrames").arraySize = walkFrames != null ? walkFrames.Length : 0;
        if (walkFrames != null)
        {
            for (var i = 0; i < walkFrames.Length; i++)
                vs.FindProperty("walkFrames").GetArrayElementAtIndex(i).objectReferenceValue = walkFrames[i];
        }

        vs.FindProperty("idleFrames").arraySize = walkFrames != null ? walkFrames.Length : 0;
        if (walkFrames != null)
        {
            for (var i = 0; i < walkFrames.Length; i++)
                vs.FindProperty("idleFrames").GetArrayElementAtIndex(i).objectReferenceValue = walkFrames[i];
        }

        vs.FindProperty("attackFrames").arraySize = attackFrames != null ? attackFrames.Length : 0;
        if (attackFrames != null)
        {
            for (var i = 0; i < attackFrames.Length; i++)
                vs.FindProperty("attackFrames").GetArrayElementAtIndex(i).objectReferenceValue = attackFrames[i];
        }
        vs.FindProperty("deathFrames").arraySize = deathFrames != null ? deathFrames.Length : 0;
        if (deathFrames != null)
        {
            for (var i = 0; i < deathFrames.Length; i++)
                vs.FindProperty("deathFrames").GetArrayElementAtIndex(i).objectReferenceValue = deathFrames[i];
        }
        vs.ApplyModifiedPropertiesWithoutUndo();

        AssignAnimatorController(root, AnimCtrlEnemy);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void BuildRangedPlantPrefab(string path, Sprite sprite, string projectilePrefabPath, Sprite[] walkFrames, Sprite[] attackFrames, Sprite[] deathFrames)
    {
        var proj = AssetDatabase.LoadAssetAtPath<Projectile>(projectilePrefabPath);
        var root = new GameObject("MvpRangedPlant");
        root.tag = "Enemy";
        root.layer = LayerMask.NameToLayer("Enemy");
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.75f, 0.95f, 1f);
        sr.sortingOrder = 2;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var box = root.AddComponent<BoxCollider2D>();
        FitBoxColliderToSprite(sr, box, 0.5f, 0.48f);
        root.AddComponent<Animator>();
        var ranged = root.AddComponent<RangedPlant>();
        var fp = new GameObject("FirePoint").transform;
        fp.SetParent(root.transform, false);
        var extX = sr.sprite != null ? sr.sprite.bounds.extents.x : 0.35f;
        fp.localPosition = new Vector3(extX * 0.85f, 0f, 0f);

        var so = new SerializedObject(ranged);
        so.FindProperty("projectilePrefab").objectReferenceValue = proj;
        so.FindProperty("firePoint").objectReferenceValue = fp;
        so.FindProperty("hitLayers").intValue =
            (1 << LayerMask.NameToLayer("Player")) |
            (1 << LayerMask.NameToLayer("Animal"));
        so.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<EnemyAI>();
        var visual = root.AddComponent<EnemySpriteAnimator>();
        var vs = new SerializedObject(visual);
        vs.FindProperty("spriteRenderer").objectReferenceValue = sr;
        vs.FindProperty("walkFrames").arraySize = walkFrames != null ? walkFrames.Length : 0;
        if (walkFrames != null)
        {
            for (var i = 0; i < walkFrames.Length; i++)
                vs.FindProperty("walkFrames").GetArrayElementAtIndex(i).objectReferenceValue = walkFrames[i];
        }

        vs.FindProperty("idleFrames").arraySize = walkFrames != null ? walkFrames.Length : 0;
        if (walkFrames != null)
        {
            for (var i = 0; i < walkFrames.Length; i++)
                vs.FindProperty("idleFrames").GetArrayElementAtIndex(i).objectReferenceValue = walkFrames[i];
        }

        vs.FindProperty("attackFrames").arraySize = attackFrames != null ? attackFrames.Length : 0;
        if (attackFrames != null)
        {
            for (var i = 0; i < attackFrames.Length; i++)
                vs.FindProperty("attackFrames").GetArrayElementAtIndex(i).objectReferenceValue = attackFrames[i];
        }
        vs.FindProperty("deathFrames").arraySize = deathFrames != null ? deathFrames.Length : 0;
        if (deathFrames != null)
        {
            for (var i = 0; i < deathFrames.Length; i++)
                vs.FindProperty("deathFrames").GetArrayElementAtIndex(i).objectReferenceValue = deathFrames[i];
        }
        vs.ApplyModifiedPropertiesWithoutUndo();

        AssignAnimatorController(root, AnimCtrlEnemy);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void BuildPlayerPrefab(string path, Sprite sprite, Sprite[] walkFrames, Sprite[] attackFrames, Sprite[] deathFrames)
    {
        var root = new GameObject("MvpPlayer");
        root.tag = "Player";
        root.layer = LayerMask.NameToLayer("Player");
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var body = root.AddComponent<BoxCollider2D>();
        FitBoxColliderToSprite(sr, body, 0.4f, 0.38f);
        root.AddComponent<Animator>();
        root.AddComponent<PlayerHealth>();
        root.AddComponent<PlayerCombat>();
        root.AddComponent<PlayerController>();
        var visualAnimator = root.AddComponent<PlayerSpriteAnimator>();

        var hitGo = new GameObject("AttackHitbox");
        hitGo.layer = LayerMask.NameToLayer("Player");
        hitGo.transform.SetParent(root.transform, false);
        var hit = hitGo.AddComponent<BoxCollider2D>();
        hit.isTrigger = true;
        var hbW = sr.sprite != null ? sr.sprite.bounds.size.x * 0.55f : 0.55f;
        hit.size = new Vector2(hbW, hbW);

        var pcSo = new SerializedObject(root.GetComponent<PlayerCombat>());
        pcSo.FindProperty("hitboxCollider").objectReferenceValue = hit;
        pcSo.FindProperty("enemyLayers").intValue = 1 << LayerMask.NameToLayer("Enemy");
        if (sr.sprite != null)
            pcSo.FindProperty("hitboxDistance").floatValue = Mathf.Max(0.55f, sr.sprite.bounds.extents.x * 0.95f);
        pcSo.ApplyModifiedPropertiesWithoutUndo();

        var vaSo = new SerializedObject(visualAnimator);
        vaSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
        vaSo.FindProperty("walkFrames").arraySize = walkFrames != null ? walkFrames.Length : 0;
        if (walkFrames != null)
        {
            for (var i = 0; i < walkFrames.Length; i++)
                vaSo.FindProperty("walkFrames").GetArrayElementAtIndex(i).objectReferenceValue = walkFrames[i];
        }

        vaSo.FindProperty("idleFrames").arraySize = walkFrames != null ? walkFrames.Length : 0;
        if (walkFrames != null)
        {
            for (var i = 0; i < walkFrames.Length; i++)
                vaSo.FindProperty("idleFrames").GetArrayElementAtIndex(i).objectReferenceValue = walkFrames[i];
        }

        vaSo.FindProperty("attackFrames").arraySize = attackFrames != null ? attackFrames.Length : 0;
        if (attackFrames != null)
        {
            for (var i = 0; i < attackFrames.Length; i++)
                vaSo.FindProperty("attackFrames").GetArrayElementAtIndex(i).objectReferenceValue = attackFrames[i];
        }
        vaSo.FindProperty("deathFrames").arraySize = deathFrames != null ? deathFrames.Length : 0;
        if (deathFrames != null)
        {
            for (var i = 0; i < deathFrames.Length; i++)
                vaSo.FindProperty("deathFrames").GetArrayElementAtIndex(i).objectReferenceValue = deathFrames[i];
        }
        vaSo.ApplyModifiedPropertiesWithoutUndo();

        AssignAnimatorController(root, AnimCtrlPlayer);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void BuildAnimalPrefab(
        string path,
        Sprite sprite,
        FarmAnimalKind kind,
        ResourceType type,
        float secondsPerTick,
        int perTick,
        int sellGold,
        Sprite[] walkFrames,
        Sprite[] deathFrames)
    {
        var root = new GameObject(Path.GetFileNameWithoutExtension(path));
        root.layer = LayerMask.NameToLayer("Animal");
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = root.AddComponent<BoxCollider2D>();
        FitBoxColliderToSprite(sr, col, 0.48f, 0.45f);
        var animal = root.AddComponent<FarmAnimal>();
        root.AddComponent<ResourceGenerator>();
        var visual = root.AddComponent<AnimalSpriteAnimator>();

        var animalSo = new SerializedObject(animal);
        animalSo.FindProperty("kind").intValue = (int)kind;
        animalSo.FindProperty("sellGoldValue").intValue = sellGold;
        animalSo.ApplyModifiedPropertiesWithoutUndo();

        var gen = root.GetComponent<ResourceGenerator>();
        var so = new SerializedObject(gen);
        so.FindProperty("resourceType").intValue = (int)type;
        so.FindProperty("amountPerTick").intValue = perTick;
        so.FindProperty("secondsPerTick").floatValue = secondsPerTick;
        so.ApplyModifiedPropertiesWithoutUndo();

        var vs = new SerializedObject(visual);
        vs.FindProperty("spriteRenderer").objectReferenceValue = sr;
        var chosenWalk = (walkFrames != null && walkFrames.Length > 0) ? walkFrames : deathFrames;
        vs.FindProperty("walkFrames").arraySize = chosenWalk != null ? chosenWalk.Length : 0;
        if (chosenWalk != null)
        {
            for (var i = 0; i < chosenWalk.Length; i++)
                vs.FindProperty("walkFrames").GetArrayElementAtIndex(i).objectReferenceValue = chosenWalk[i];
        }

        vs.FindProperty("idleFrames").arraySize = chosenWalk != null ? chosenWalk.Length : 0;
        if (chosenWalk != null)
        {
            for (var i = 0; i < chosenWalk.Length; i++)
                vs.FindProperty("idleFrames").GetArrayElementAtIndex(i).objectReferenceValue = chosenWalk[i];
        }

        vs.FindProperty("deathFrames").arraySize = deathFrames != null ? deathFrames.Length : 0;
        if (deathFrames != null)
        {
            for (var i = 0; i < deathFrames.Length; i++)
                vs.FindProperty("deathFrames").GetArrayElementAtIndex(i).objectReferenceValue = deathFrames[i];
        }

        vs.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<Animator>();
        AssignAnimatorController(root, AnimCtrlFarmAnimal);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void AssignAnimatorController(GameObject root, string controllerAssetPath)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerAssetPath);
        if (ctrl == null)
        {
            Debug.LogWarning($"[DarkFarmMvpBuilder] Missing Animator controller at {controllerAssetPath}");
            return;
        }

        var anim = root.GetComponent<Animator>();
        if (anim == null)
            anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
    }

    /// <summary>Scene objects matching documented setup: Collider2D bounds + CorralZone + spawn empties.</summary>
    private static void CreateCorral(
        string objectName,
        FarmAnimalKind kind,
        Vector3 worldCenter,
        Vector2 colliderSize,
        int maxAnimals)
    {
        var go = new GameObject(objectName);
        go.transform.position = worldCenter;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = colliderSize;
        box.offset = Vector2.zero;

        var zone = go.AddComponent<CorralZone>();
        var zSo = new SerializedObject(zone);
        zSo.FindProperty("allowedKind").intValue = (int)kind;
        zSo.FindProperty("maxAnimals").intValue = maxAnimals;
        zSo.FindProperty("area").objectReferenceValue = box;
        zSo.ApplyModifiedPropertiesWithoutUndo();

        var hx = colliderSize.x * 0.32f;
        var hy = colliderSize.y * 0.32f;

        var spA = new GameObject("SpawnPoint");
        spA.transform.SetParent(go.transform, false);
        spA.transform.localPosition = new Vector3(-hx, hy * 0.55f, 0f);

        var spB = new GameObject("SpawnPoint (1)");
        spB.transform.SetParent(go.transform, false);
        spB.transform.localPosition = new Vector3(hx, -hy * 0.55f, 0f);
    }

    private static void CreateGranero(Vector3 position, ShopUI shopUiRef)
    {
        var barn = new GameObject("Granero");
        barn.transform.position = position;

        var box = barn.AddComponent<BoxCollider2D>();
        box.size = new Vector2(3.4f, 2.6f);

        var trig = barn.AddComponent<BarnShopTrigger>();
        var tSo = new SerializedObject(trig);
        tSo.FindProperty("shopUi").objectReferenceValue = shopUiRef;
        tSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildMainScene(
        Sprite groundSprite,
        string playerPrefabPath,
        string plantPrefabPath,
        string rangedPrefabPath,
        string cowPrefabPath,
        string chickenPrefabPath,
        string pigPrefabPath)
    {
        var ground = new GameObject("Ground");
        var gsr = ground.AddComponent<SpriteRenderer>();
        gsr.sprite = groundSprite;
        gsr.drawMode = SpriteDrawMode.Tiled;
        gsr.size = new Vector2(40f, 40f);
        gsr.sortingOrder = -20;
        gsr.color = new Color(0.55f, 0.55f, 0.65f);
        ground.transform.localScale = Vector3.one;

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9.5f;
        cam.backgroundColor = new Color(0.06f, 0.05f, 0.09f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();

        var lightGo = new GameObject("Global Light 2D");
        var l2d = lightGo.AddComponent<Light2D>();
        l2d.lightType = Light2D.LightType.Global;
        l2d.intensity = 0.4f;
        l2d.color = new Color(0.55f, 0.52f, 0.78f);

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        var bootstrap = new GameObject("Bootstrap");
        bootstrap.AddComponent<WorldSettings>();
        bootstrap.AddComponent<SceneRuntimeWiring>();

        var systems = new GameObject("GameSystems");
        systems.AddComponent<GameManager>();
        systems.AddComponent<TimeManager>();
        systems.AddComponent<InventorySystem>();
        var shop = systems.AddComponent<ShopSystem>();

        var farmSystems = new GameObject("[Systems]");
        var corralMgr = farmSystems.AddComponent<CorralManager>();
        var animalSpawnerComp = farmSystems.AddComponent<AnimalSpawner>();
        var aspFarmSo = new SerializedObject(animalSpawnerComp);
        aspFarmSo.FindProperty("corralManager").objectReferenceValue = corralMgr;
        aspFarmSo.ApplyModifiedPropertiesWithoutUndo();

        CreateCorral("Corral_Vacas", FarmAnimalKind.Cow, new Vector3(-12f, 3f, 0f), new Vector2(6f, 5f), 8);
        CreateCorral("Corral_Pollos", FarmAnimalKind.Chicken, new Vector3(-12f, -2.5f, 0f), new Vector2(6f, 5f), 10);
        CreateCorral("Corral_Cerdos", FarmAnimalKind.Pig, new Vector3(-4f, 3f, 0f), new Vector2(6f, 5f), 8);

        var shopSo = new SerializedObject(shop);
        shopSo.FindProperty("inventory").objectReferenceValue = systems.GetComponent<InventorySystem>();
        shopSo.FindProperty("animalSpawner").objectReferenceValue = animalSpawnerComp;
        shopSo.FindProperty("cowPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(cowPrefabPath);
        shopSo.FindProperty("chickenPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(chickenPrefabPath);
        shopSo.FindProperty("pigPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(pigPrefabPath);
        shopSo.ApplyModifiedPropertiesWithoutUndo();

        var spawnGo = new GameObject("SpawnManager");
        var spawn = spawnGo.AddComponent<SpawnManager>();
        var plant = AssetDatabase.LoadAssetAtPath<EnemyBase>(plantPrefabPath);
        var ranged = AssetDatabase.LoadAssetAtPath<EnemyBase>(rangedPrefabPath);
        var spawnSo = new SerializedObject(spawn);
        var arr = spawnSo.FindProperty("enemyPrefabs");
        arr.arraySize = 2;
        arr.GetArrayElementAtIndex(0).objectReferenceValue = plant;
        arr.GetArrayElementAtIndex(1).objectReferenceValue = ranged;
        spawnSo.FindProperty("timeManager").objectReferenceValue = systems.GetComponent<TimeManager>();
        spawnSo.FindProperty("corralManager").objectReferenceValue = corralMgr;
        spawnSo.ApplyModifiedPropertiesWithoutUndo();

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(0f, -1f, 0f);

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)canvasGo.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                   Resources.GetBuiltinResource<Font>("Arial.ttf");

        var hudBar = CreateUiObject("ResourceHudBar", canvasGo.transform);
        var hudRt = hudBar.GetComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 0.88f);
        hudRt.anchorMax = new Vector2(1f, 1f);
        hudRt.offsetMin = hudRt.offsetMax = Vector2.zero;
        hudBar.AddComponent<Image>().color = new Color(0.03f, 0.025f, 0.06f, 0.9f);

        var milkT = CreateHudStatText("MilkHud", hudBar.transform, font, new Vector2(0.02f, 0f), new Vector2(0.33f, 1f), "Leche 0");
        var eggT = CreateHudStatText("EggHud", hudBar.transform, font, new Vector2(0.345f, 0f), new Vector2(0.655f, 1f), "Huevos 0");
        var meatT = CreateHudStatText("MeatHud", hudBar.transform, font, new Vector2(0.67f, 0f), new Vector2(0.98f, 1f), "Carne 0");

        var sliderGo = CreateUiObject("HealthSlider", canvasGo.transform);
        var rt = sliderGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.02f, 0.775f);
        rt.anchorMax = new Vector2(0.42f, 0.865f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var fillGo = CreateUiObject("Fill", sliderGo.transform);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        fillGo.AddComponent<Image>().color = new Color(0.55f, 0.15f, 0.18f, 1f);
        var slider = sliderGo.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.minValue = 0f;
        slider.maxValue = 10f;
        slider.value = 10f;
        sliderGo.AddComponent<HealthBar>();

        var uiRoot = new GameObject("UIManager");
        uiRoot.transform.SetParent(canvasGo.transform, false);
        var ui = uiRoot.AddComponent<UIManager>();
        var res = uiRoot.AddComponent<ResourceUI>();
        var resSo = new SerializedObject(res);
        resSo.FindProperty("milkText").objectReferenceValue = milkT;
        resSo.FindProperty("eggText").objectReferenceValue = eggT;
        resSo.FindProperty("meatText").objectReferenceValue = meatT;
        resSo.ApplyModifiedPropertiesWithoutUndo();

        var shopPivot = CreateUiObject("ShopUIPivot", canvasGo.transform);
        var shopPivotRt = shopPivot.GetComponent<RectTransform>();
        shopPivotRt.anchorMin = Vector2.zero;
        shopPivotRt.anchorMax = Vector2.one;
        shopPivotRt.offsetMin = shopPivotRt.offsetMax = Vector2.zero;

        var shopPanel = new GameObject("ShopPanel");
        shopPanel.transform.SetParent(shopPivot.transform, false);
        var sprt = shopPanel.AddComponent<RectTransform>();
        sprt.anchorMin = new Vector2(0.55f, 0.15f);
        sprt.anchorMax = new Vector2(0.98f, 0.85f);
        sprt.offsetMin = sprt.offsetMax = Vector2.zero;
        var bg = shopPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.12f, 0.92f);
        shopPanel.SetActive(false);

        var shopUiComp = shopPivot.AddComponent<ShopUI>();

        var cowCostTx = CreateText("CowCostText", shopPanel.transform, font, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
        cowCostTx.fontSize = 16;
        cowCostTx.text = "Vaca:";
        var chkCostTx = CreateText("ChickenCostText", shopPanel.transform, font, new Vector2(0.05f, 0.835f), new Vector2(0.95f, 0.885f));
        chkCostTx.fontSize = 16;
        chkCostTx.text = "Gallina:";
        var pigCostTx = CreateText("PigCostText", shopPanel.transform, font, new Vector2(0.05f, 0.77f), new Vector2(0.95f, 0.82f));
        pigCostTx.fontSize = 16;
        pigCostTx.text = "Cerdo:";
        var atkCostTx = CreateText("AttackCostText", shopPanel.transform, font, new Vector2(0.05f, 0.705f), new Vector2(0.95f, 0.755f));
        atkCostTx.fontSize = 16;
        atkCostTx.text = "Ataque:";
        var spdCostTx = CreateText("SpeedCostText", shopPanel.transform, font, new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.69f));
        spdCostTx.fontSize = 16;
        spdCostTx.text = "Velocidad:";

        var shopUiBindSo = new SerializedObject(shopUiComp);
        shopUiBindSo.FindProperty("panelRoot").objectReferenceValue = shopPanel;
        shopUiBindSo.FindProperty("shopSystem").objectReferenceValue = shop;
        shopUiBindSo.FindProperty("inventory").objectReferenceValue = systems.GetComponent<InventorySystem>();
        shopUiBindSo.FindProperty("cowCostText").objectReferenceValue = cowCostTx;
        shopUiBindSo.FindProperty("chickenCostText").objectReferenceValue = chkCostTx;
        shopUiBindSo.FindProperty("pigCostText").objectReferenceValue = pigCostTx;
        shopUiBindSo.FindProperty("attackCostText").objectReferenceValue = atkCostTx;
        shopUiBindSo.FindProperty("speedCostText").objectReferenceValue = spdCostTx;
        shopUiBindSo.ApplyModifiedPropertiesWithoutUndo();

        var binder = shopPanel.AddComponent<ShopPanelBinder>();
        var buyCow = CreateShopButton("BuyCow", shopPanel.transform, font, "Comprar vaca", new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.615f));
        var buyChk = CreateShopButton("BuyChicken", shopPanel.transform, font, "Comprar gallina", new Vector2(0.05f, 0.465f), new Vector2(0.95f, 0.535f));
        var buyPig = CreateShopButton("BuyPig", shopPanel.transform, font, "Comprar cerdo", new Vector2(0.05f, 0.39f), new Vector2(0.95f, 0.455f));
        var buyAtk = CreateShopButton("BuyAttackUpgrade", shopPanel.transform, font, "Mejorar ataque", new Vector2(0.05f, 0.315f), new Vector2(0.95f, 0.38f));
        var buySpd = CreateShopButton("BuySpeedUpgrade", shopPanel.transform, font, "Mejorar velocidad", new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.305f));
        var closeShop = CreateShopButton("CloseShop", shopPanel.transform, font, "Cerrar granero", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.105f));

        var binderSo = new SerializedObject(binder);
        binderSo.FindProperty("ui").objectReferenceValue = ui;
        binderSo.FindProperty("buyCow").objectReferenceValue = buyCow;
        binderSo.FindProperty("buyChicken").objectReferenceValue = buyChk;
        binderSo.FindProperty("buyPig").objectReferenceValue = buyPig;
        binderSo.FindProperty("buyAttackUpgrade").objectReferenceValue = buyAtk;
        binderSo.FindProperty("buySpeedUpgrade").objectReferenceValue = buySpd;
        binderSo.FindProperty("closeShop").objectReferenceValue = closeShop;
        binderSo.ApplyModifiedPropertiesWithoutUndo();

        var uiSo = new SerializedObject(ui);
        uiSo.FindProperty("healthBar").objectReferenceValue = sliderGo.GetComponent<HealthBar>();
        uiSo.FindProperty("resourceUI").objectReferenceValue = res;
        uiSo.FindProperty("shopPanelRoot").objectReferenceValue = shopPanel;
        uiSo.FindProperty("shopUi").objectReferenceValue = shopUiComp;
        uiSo.FindProperty("shopSystem").objectReferenceValue = shop;
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        var hbSo = new SerializedObject(sliderGo.GetComponent<HealthBar>());
        hbSo.FindProperty("slider").objectReferenceValue = slider;
        hbSo.ApplyModifiedPropertiesWithoutUndo();

        var gmSo = new SerializedObject(systems.GetComponent<GameManager>());
        gmSo.FindProperty("timeManager").objectReferenceValue = systems.GetComponent<TimeManager>();
        gmSo.FindProperty("inventorySystem").objectReferenceValue = systems.GetComponent<InventorySystem>();
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        var invSo = new SerializedObject(systems.GetComponent<InventorySystem>());
        invSo.FindProperty("startingMilk").intValue = 12;
        invSo.FindProperty("startingEgg").intValue = 14;
        invSo.FindProperty("startingMeat").intValue = 10;
        invSo.ApplyModifiedPropertiesWithoutUndo();

        CreateGranero(new Vector3(10f, 0f, 0f), shopUiComp);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Text CreateHudStatText(string name, Transform parent, Font font, Vector2 anchorMin, Vector2 anchorMax, string initialLine)
    {
        var go = CreateUiObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(8f, 4f);
        rt.offsetMax = new Vector2(-8f, -10f);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = 22;
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(1f, 0.96f, 0.88f);
        t.text = initialLine;
        t.alignment = TextAnchor.MiddleLeft;
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.88f);
        ol.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    private static Text CreateText(string name, Transform parent, Font font, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = CreateUiObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = 22;
        t.color = Color.white;
        t.text = "0";
        t.alignment = TextAnchor.MiddleLeft;
        return t;
    }

    private static Button CreateShopButton(
        string name,
        Transform parent,
        Font font,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        var go = CreateUiObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(8, 4);
        rt.offsetMax = new Vector2(-8, -4);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.18f, 0.28f, 1f);
        var btn = go.AddComponent<Button>();
        var textGo = CreateUiObject("Text", go.transform);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = textGo.AddComponent<Text>();
        tx.font = font;
        tx.fontSize = 20;
        tx.color = Color.white;
        tx.text = label;
        tx.alignment = TextAnchor.MiddleCenter;
        btn.targetGraphic = img;
        return btn;
    }

    private static void BuildMenuScene()
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.05f, 0.04f, 0.08f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)canvasGo.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                   Resources.GetBuiltinResource<Font>("Arial.ttf");
        var title = CreateText("Title", canvasGo.transform, font, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f));
        title.text = "Dark Farm Survival";
        title.fontSize = 42;
        title.alignment = TextAnchor.MiddleCenter;

        var btnGo = CreateUiObject("StartButton", canvasGo.transform);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.35f, 0.38f);
        rt.anchorMax = new Vector2(0.65f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        btnGo.AddComponent<Image>().color = new Color(0.25f, 0.2f, 0.35f, 1f);
        var btn = btnGo.AddComponent<Button>();
        var txGo = CreateUiObject("Text", btnGo.transform);
        var trt = txGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = txGo.AddComponent<Text>();
        tx.font = font;
        tx.fontSize = 28;
        tx.color = Color.white;
        tx.text = "Start Game";
        tx.alignment = TextAnchor.MiddleCenter;
        btn.targetGraphic = btnGo.GetComponent<Image>();

        var menuBoot = canvasGo.AddComponent<MenuSceneBootstrap>();
        var mso = new SerializedObject(menuBoot);
        mso.FindProperty("startButton").objectReferenceValue = btn;
        mso.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildTestScene(Sprite groundSprite)
    {
        var ground = new GameObject("TestGround");
        var gsr = ground.AddComponent<SpriteRenderer>();
        gsr.sprite = groundSprite;
        gsr.drawMode = SpriteDrawMode.Tiled;
        gsr.size = new Vector2(24f, 24f);
        gsr.sortingOrder = -10;

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();
    }

    private static void ApplyBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{SceneDir}/scene_01_menu.unity", true),
            new EditorBuildSettingsScene($"{SceneDir}/scene_00_main.unity", true),
            new EditorBuildSettingsScene($"{SceneDir}/scene_02_test.unity", true)
        };
    }
}
#endif
