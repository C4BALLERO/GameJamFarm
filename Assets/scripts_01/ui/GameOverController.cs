using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Cuando el <see cref="BarnHealth"/> llega a 0, muestra game over y detiene el juego.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameOverController : MonoBehaviour
{
    [SerializeField] private BarnHealth barnHealth;
    [SerializeField] private Canvas canvasOverride;

    private GameObject _panelRoot;
    private bool _shown;
    private bool _boundToBarn;

    private void Start()
    {
        TryBindBarnHealth();
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_shown || _boundToBarn)
            return;
        TryBindBarnHealth();
    }

    private void TryBindBarnHealth()
    {
        if (barnHealth == null)
            barnHealth = FindFirstObjectByType<BarnHealth>();
        if (barnHealth == null)
            return;

        barnHealth.Died -= OnBarnDied;
        barnHealth.Died += OnBarnDied;
        _boundToBarn = true;
    }

    private void OnDestroy()
    {
        if (barnHealth != null)
            barnHealth.Died -= OnBarnDied;
    }

    private void OnBarnDied()
    {
        if (_shown)
            return;
        _shown = true;

        if (GameManager.Instance != null)
            GameManager.Instance.EnterGameOver();
        else
            Time.timeScale = 0f;

        EnsureUiExists();
        if (_panelRoot != null)
            _panelRoot.SetActive(true);
    }

    private void EnsureUiExists()
    {
        if (_panelRoot != null)
            return;

        var canvasGo = canvasOverride != null ? canvasOverride.gameObject : GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cv = canvasGo.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 5000;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        _panelRoot = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        _panelRoot.transform.SetParent(canvasGo.transform, false);
        var rt = _panelRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var bg = _panelRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(_panelRoot.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.58f);
        titleRt.anchorMax = new Vector2(0.5f, 0.58f);
        titleRt.sizeDelta = new Vector2(900f, 120f);
        var titleTxt = titleGo.GetComponent<Text>();
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleTxt.fontSize = 56;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = Color.white;
        titleTxt.text = "GAME OVER";

        var subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
        subGo.transform.SetParent(_panelRoot.transform, false);
        var subRt = subGo.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.48f);
        subRt.anchorMax = new Vector2(0.5f, 0.48f);
        subRt.sizeDelta = new Vector2(900f, 60f);
        var subTxt = subGo.GetComponent<Text>();
        subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        subTxt.fontSize = 22;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color = new Color(0.9f, 0.85f, 0.85f);
        subTxt.text = "El granero fue destruido";

        CreateButton(_panelRoot.transform, "BtnRestart", new Vector2(0.5f, 0.36f), new Vector2(320f, 56f), "Reiniciar", Restart);
        CreateButton(_panelRoot.transform, "BtnMenu", new Vector2(0.5f, 0.26f), new Vector2(320f, 56f), "Menu principal", Menu);
    }

    private static void CreateButton(Transform parent, string name, Vector2 anchorCenterNorm, Vector2 size, string label, UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorCenterNorm;
        rt.anchorMax = anchorCenterNorm;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.35f, 0.15f, 0.18f, 0.95f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var txt = txtGo.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 22;
    }

    public void Restart()
    {
        GameManager.Instance?.RestartFromGameOver();
    }

    public void Menu()
    {
        GameManager.Instance?.QuitToMainMenuFromGameOver();
    }
}
