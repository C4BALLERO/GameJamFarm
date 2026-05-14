using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class PostProcessFilterController : MonoBehaviour
{
    public enum FilterType
    {
        GrayscaleLuma = 0,
        Pixelation = 1,
        EdgeDetection = 2,
        Invert = 3,
        Sepia = 4
    }

    [Header("Filter Selection")]
    [SerializeField] private FilterType activeFilter = FilterType.GrayscaleLuma;

    [Header("Materials (assign in Inspector)")]
    [SerializeField] private Material grayscaleMaterial;
    [SerializeField] private Material pixelationMaterial;
    [SerializeField] private Material edgeMaterial;
    [SerializeField] private Material invertMaterial;
    [SerializeField] private Material sepiaMaterial;

    [Header("Pixelation")]
    [Min(1f)]
    [SerializeField] private float pixelSize = 8f;

    [Header("Edge Detection")]
    [Range(0f, 2f)]
    [SerializeField] private float edgeThreshold = 0.2f;
    [Range(0f, 5f)]
    [SerializeField] private float edgeIntensity = 1.0f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) activeFilter = FilterType.GrayscaleLuma;
        if (Input.GetKeyDown(KeyCode.Alpha2)) activeFilter = FilterType.Pixelation;
        if (Input.GetKeyDown(KeyCode.Alpha3)) activeFilter = FilterType.EdgeDetection;
        if (Input.GetKeyDown(KeyCode.Alpha4)) activeFilter = FilterType.Invert;
        if (Input.GetKeyDown(KeyCode.Alpha5)) activeFilter = FilterType.Sepia;

        if (pixelationMaterial != null)
        {
            pixelationMaterial.SetFloat("_PixelSize", pixelSize);
        }

        if (edgeMaterial != null)
        {
            edgeMaterial.SetFloat("_EdgeThreshold", edgeThreshold);
            edgeMaterial.SetFloat("_EdgeIntensity", edgeIntensity);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Material current = GetCurrentMaterial();
        if (current == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Graphics.Blit(source, destination, current);
    }

    private Material GetCurrentMaterial()
    {
        switch (activeFilter)
        {
            case FilterType.GrayscaleLuma:
                return grayscaleMaterial;
            case FilterType.Pixelation:
                return pixelationMaterial;
            case FilterType.EdgeDetection:
                return edgeMaterial;
            case FilterType.Invert:
                return invertMaterial;
            case FilterType.Sepia:
                return sepiaMaterial;
            default:
                return null;
        }
    }
}
