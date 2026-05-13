#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Asigna MP3 a prefabs de animales. Vaca: <c>vaca.mp3</c> (respaldo Caminando en resources_08).
/// Gallina: Gallina + galliaMuere | Cerdo: Cerdo
/// </summary>
public static class WireFarmAnimalAudioClips
{
    private const string CowPrefab = "Assets/prefabs_02/animals/MvpFarmAnimal_Cow.prefab";
    private const string ChickenPrefab = "Assets/prefabs_02/animals/MvpFarmAnimal_Chicken.prefab";
    private const string PigPrefab = "Assets/prefabs_02/animals/MvpFarmAnimal_Pig.prefab";

    private const string ClipVaca = "Assets/resources/AnimalSounds/vaca.mp3";
    private const string ClipCaminando = "Assets/resources_08/Caminando.mp3";
    private const string ClipGallina = "Assets/resources_08/Gallina.mp3";
    private const string ClipGallinaMuere = "Assets/resources_08/galliaMuere.mp3";
    private const string ClipCerdo = "Assets/resources_08/Cerdo.mp3";

    [MenuItem("Tools/Audio/Assign Farm Animal Clips (resources_08)")]
    private static void Assign()
    {
        var vaca = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipVaca);
        var caminando = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipCaminando);
        var gallina = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipGallina);
        var gallinaMuere = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipGallinaMuere);
        var cerdo = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipCerdo);

        if ((vaca == null && caminando == null) || gallina == null || gallinaMuere == null || cerdo == null)
        {
            Debug.LogWarning("[WireAnimalAudio] Falta algún MP3. Vaca: AnimalSounds/vaca o resources_08/Caminando. Gallina/Cerdo en resources_08.");
        }

        WireCowPrefabVacaOnly(vaca, caminando);
        WirePrefab(ChickenPrefab, gallina, gallinaMuere);
        WirePrefab(PigPrefab, cerdo, null);

        AssetDatabase.SaveAssets();
        Debug.Log("[WireAnimalAudio] Prefabs de animales actualizados con audio.");
    }

    private static void WireCowPrefabVacaOnly(AudioClip vacaClip, AudioClip caminandoFallback)
    {
        var ambient = vacaClip != null ? vacaClip : caminandoFallback;
        var root = PrefabUtility.LoadPrefabContents(CowPrefab);
        try
        {
            if (root.GetComponent<AudioSource>() == null)
                root.AddComponent<AudioSource>();

            var audio = root.GetComponent<FarmAnimalAudio>();
            if (audio == null)
                audio = root.AddComponent<FarmAnimalAudio>();

            var so = new SerializedObject(audio);
            so.FindProperty("ambientClips").arraySize = 0;
            so.FindProperty("ambientLoopClip").objectReferenceValue = ambient;
            so.FindProperty("deathClip").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        finally
        {
            PrefabUtility.SaveAsPrefabAsset(root, CowPrefab);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void WirePrefab(string prefabPath, AudioClip ambient, AudioClip death)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root.GetComponent<AudioSource>() == null)
                root.AddComponent<AudioSource>();

            var audio = root.GetComponent<FarmAnimalAudio>();
            if (audio == null)
                audio = root.AddComponent<FarmAnimalAudio>();

            var so = new SerializedObject(audio);
            so.FindProperty("ambientLoopClip").objectReferenceValue = ambient;
            so.FindProperty("deathClip").objectReferenceValue = death;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        finally
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
