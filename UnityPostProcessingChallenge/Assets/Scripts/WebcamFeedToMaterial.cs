using UnityEngine;

public class WebcamFeedToMaterial : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string requestedDeviceName = "";
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFPS = 30;

    private WebCamTexture webcamTexture;

    private void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogWarning("No webcam devices found.");
            return;
        }

        string selected = devices[0].name;
        if (!string.IsNullOrWhiteSpace(requestedDeviceName))
        {
            foreach (WebCamDevice d in devices)
            {
                if (d.name == requestedDeviceName)
                {
                    selected = d.name;
                    break;
                }
            }
        }

        webcamTexture = new WebCamTexture(selected, requestedWidth, requestedHeight, requestedFPS);
        webcamTexture.Play();

        if (targetRenderer != null)
        {
            targetRenderer.material.mainTexture = webcamTexture;
        }
    }

    private void OnDisable()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
