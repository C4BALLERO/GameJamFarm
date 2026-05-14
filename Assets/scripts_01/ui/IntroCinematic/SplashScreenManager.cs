using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Pantalla de splash inicial: fondo oscuro, logo con fade in / hold / fade out y carga de la siguiente escena (p. ej. historia).
/// Tiempo total ≈ fadeIn + hold + fadeOut (por defecto ~4,2 s).
/// </summary>
[DisallowMultipleComponent]
public sealed class SplashScreenManager : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "scene_01_menu";
    [SerializeField] private string logoResourcePath = "Splash/dark_farm_splash";

    [Header("Visual")]
    [SerializeField] private Color backgroundColor = new Color32(0x12, 0x0C, 0x10, 0xFF);
    [SerializeField] private float logoDriftAmplitudePixels = 5f;

    [Header("Timing (unscaled)")]
    [SerializeField] private float fadeInDuration = 1.15f;
    [SerializeField] private float visibleHoldDuration = 2.1f;
    [SerializeField] private float fadeOutDuration = 1f;

    private CanvasGroup _logoGroup;
    private RectTransform _logoRoot;
    private Vector2 _logoBaseAnchored;

    private void Start()
    {
        EnsureEventSystem();
        BuildUi();
        StartCoroutine(RunSplash());
    }

    private IEnumerator RunSplash()
    {
        _logoGroup.alpha = 0f;
        var fadeIn = Mathf.Max(0.05f, fadeInDuration);
        var hold = Mathf.Max(0.1f, visibleHoldDuration);
        var fadeOut = Mathf.Max(0.05f, fadeOutDuration);

        yield return FadeGroup(_logoGroup, 0f, 1f, fadeIn);

        var elapsed = 0f;
        while (elapsed < hold)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyLogoDrift();
            yield return null;
        }

        yield return FadeGroup(_logoGroup, _logoGroup.alpha, 0f, fadeOut);

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("[SplashScreenManager] nextSceneName is empty.");
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void ApplyLogoDrift()
    {
        if (_logoRoot == null)
            return;
        var t = Time.unscaledTime;
        var amp = logoDriftAmplitudePixels;
        var wobble = new Vector2(
            Mathf.Sin(t * 0.72f) * amp,
            Mathf.Cos(t * 0.55f) * amp * 0.45f);
        _logoRoot.anchoredPosition = _logoBaseAnchored + wobble;
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;
        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var k = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        group.alpha = to;
    }

    private void BuildUi()
    {
        var canvasGo = new GameObject("SplashCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        var bg = CreateUiImage(canvasGo.transform, "SplashBackground", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.sprite = null;
        bg.color = backgroundColor;
        bg.raycastTarget = false;

        var logoRootGo = new GameObject("LogoRoot", typeof(RectTransform), typeof(CanvasGroup));
        logoRootGo.transform.SetParent(canvasGo.transform, false);
        _logoRoot = logoRootGo.GetComponent<RectTransform>();
        _logoRoot.anchorMin = new Vector2(0.08f, 0.12f);
        _logoRoot.anchorMax = new Vector2(0.92f, 0.88f);
        _logoRoot.offsetMin = _logoRoot.offsetMax = Vector2.zero;
        _logoBaseAnchored = _logoRoot.anchoredPosition;

        _logoGroup = logoRootGo.GetComponent<CanvasGroup>();
        _logoGroup.blocksRaycasts = false;
        _logoGroup.interactable = false;

        var logoSprite = LoadSplashSprite();
        if (logoSprite == null)
            Debug.LogError("[SplashScreenManager] Missing sprite/texture at Resources path: " + logoResourcePath);

        var logoImg = CreateUiImage(logoRootGo.transform, "Logo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        logoImg.sprite = logoSprite;
        logoImg.type = Image.Type.Simple;
        logoImg.preserveAspect = true;
        logoImg.color = Color.white;
        logoImg.raycastTarget = false;
    }

    private Sprite LoadSplashSprite()
    {
        var sprite = Resources.Load<Sprite>(logoResourcePath);
        if (sprite != null)
            return sprite;

        var texture = Resources.Load<Texture2D>(logoResourcePath);
        if (texture == null)
            return null;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private static Image CreateUiImage(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go.GetComponent<Image>();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }
}
