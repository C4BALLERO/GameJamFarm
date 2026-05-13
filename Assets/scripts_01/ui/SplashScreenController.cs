using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen splash with a timed progress bar, then loads the menu scene.
/// </summary>
[DisallowMultipleComponent]
public sealed class SplashScreenController : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "scene_01_menu";
    [SerializeField] private float displayDurationSeconds = 4f;
    [SerializeField] private string splashSpriteResourcePath = "Splash/dark_farm_splash";

    private Image _barFill;

    private void Start()
    {
        BuildUi();
        StartCoroutine(RunSplash());
    }

    private IEnumerator RunSplash()
    {
        var duration = Mathf.Max(0.01f, displayDurationSeconds);
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (_barFill != null)
                _barFill.fillAmount = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        if (_barFill != null)
            _barFill.fillAmount = 1f;

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("[SplashScreenController] Next scene name is empty.");
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
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

        var splashSprite = LoadSplashSprite();
        if (splashSprite == null)
            Debug.LogError("[SplashScreenController] Missing texture at Resources path: " + splashSpriteResourcePath);

        var bg = CreateUiImage(canvasGo.transform, "SplashBackground", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.sprite = splashSprite;
        bg.type = Image.Type.Simple;
        bg.preserveAspect = true;
        bg.color = Color.white;

        var barRootGo = new GameObject("LoadingBarRoot", typeof(RectTransform), typeof(Image));
        barRootGo.transform.SetParent(canvasGo.transform, false);
        var barRootRect = barRootGo.GetComponent<RectTransform>();
        barRootRect.anchorMin = new Vector2(0.5f, 0f);
        barRootRect.anchorMax = new Vector2(0.5f, 0f);
        barRootRect.pivot = new Vector2(0.5f, 0f);
        barRootRect.sizeDelta = new Vector2(280f, 14f);
        barRootRect.anchoredPosition = new Vector2(0f, 48f);
        var barRoot = barRootGo.GetComponent<Image>();
        barRoot.color = new Color(0.12f, 0.1f, 0.1f, 0.92f);

        var barFillGo = new GameObject("LoadingBarFill", typeof(RectTransform), typeof(Image));
        barFillGo.transform.SetParent(barRootGo.transform, false);
        var fillRect = barFillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        _barFill = barFillGo.GetComponent<Image>();
        _barFill.type = Image.Type.Filled;
        _barFill.fillMethod = Image.FillMethod.Horizontal;
        _barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _barFill.fillAmount = 0f;
        _barFill.color = new Color(0.55f, 0.42f, 0.22f, 1f);
    }

    private Sprite LoadSplashSprite()
    {
        var sprite = Resources.Load<Sprite>(splashSpriteResourcePath);
        if (sprite != null)
            return sprite;

        var texture = Resources.Load<Texture2D>(splashSpriteResourcePath);
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
}
