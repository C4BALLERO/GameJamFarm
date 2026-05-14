using UnityEditor;
using UnityEngine;

public static class SetupPostFXChallenge
{
    private const string MaterialsFolder = "Assets/Materials";

    [MenuItem("Tools/PostFX Challenge/Setup Scene")]
    public static void SetupScene()
    {
        EnsureFolder("Assets", "Materials");

        Material grayscale = CreateOrLoadMaterial("GrayscaleLuma", "Custom/PostFX/GrayscaleLuma");
        Material pixel = CreateOrLoadMaterial("Pixelation", "Custom/PostFX/Pixelation");
        Material edge = CreateOrLoadMaterial("EdgeSobel", "Custom/PostFX/EdgeSobel");
        Material invert = CreateOrLoadMaterial("Invert", "Custom/PostFX/Invert");
        Material sepia = CreateOrLoadMaterial("Sepia", "Custom/PostFX/Sepia");

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }

        PostProcessFilterController controller = cam.GetComponent<PostProcessFilterController>();
        if (controller == null)
        {
            controller = cam.gameObject.AddComponent<PostProcessFilterController>();
        }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("grayscaleMaterial").objectReferenceValue = grayscale;
        so.FindProperty("pixelationMaterial").objectReferenceValue = pixel;
        so.FindProperty("edgeMaterial").objectReferenceValue = edge;
        so.FindProperty("invertMaterial").objectReferenceValue = invert;
        so.FindProperty("sepiaMaterial").objectReferenceValue = sepia;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(cam.gameObject);
        AssetDatabase.SaveAssets();

        Debug.Log("PostFX Challenge setup completed. Press Play and use keys 1-5.");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Material CreateOrLoadMaterial(string fileName, string shaderName)
    {
        string matPath = MaterialsFolder + "/" + fileName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError("Shader not found: " + shaderName);
            return null;
        }

        mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }
}
