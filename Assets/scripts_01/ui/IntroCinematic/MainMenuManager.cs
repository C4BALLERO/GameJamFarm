using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Añade «Opciones» y «Salir» al menú principal y un panel de opciones mínimo (placeholder).
/// Se engancha al mismo GameObject que <see cref="MenuSceneBootstrap"/> (Canvas raíz).
/// </summary>
[DisallowMultipleComponent]
public sealed class MainMenuManager : MonoBehaviour
{
    [SerializeField] private bool buildExtraButtonsIfMissing = true;

    private Canvas _canvas;
    private GameObject _optionsRoot;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
        if (_canvas == null)
            _canvas = Object.FindFirstObjectByType<Canvas>();
    }

    private void Start()
    {
        if (!buildExtraButtonsIfMissing || _canvas == null)
            return;
        if (_canvas.transform.Find("MainMenu_ExtraRow") != null)
            return;

        EnsureExtraButtons();
    }

    private void EnsureExtraButtons()
    {
        var row = new GameObject("MainMenu_ExtraRow", typeof(RectTransform));
        row.transform.SetParent(_canvas.transform, false);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0f);
        rowRt.anchorMax = new Vector2(0.5f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.sizeDelta = new Vector2(720f, 64f);
        rowRt.anchoredPosition = new Vector2(0f, 28f);

        CreateMenuButton(row.transform, "OptionsButton", "Options", -130f, OpenOptions);
        CreateMenuButton(row.transform, "ExitButton", "Exit", 130f, QuitApplication);
    }

    private static void CreateMenuButton(Transform parent, string objectName, string label, float xOffset, UnityAction onClick)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 52f);
        rt.anchoredPosition = new Vector2(xOffset, 0f);

        var img = go.GetComponent<Image>();
        UIStyleSheet.ApplyButtonGraphic(img, UIStyleSheet.ButtonNormal);
        var btn = go.GetComponent<Button>();
        UIStyleSheet.ApplyButtonStates(btn);
        btn.onClick.AddListener(onClick);

        var cap = new GameObject("Label", typeof(RectTransform), typeof(Text));
        cap.transform.SetParent(go.transform, false);
        var crt = cap.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(6f, 4f);
        crt.offsetMax = new Vector2(-6f, -4f);
        var tx = cap.GetComponent<Text>();
        tx.text = label;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.font = UIStyleSheet.GetUiFont();
        tx.fontSize = 20;
        tx.color = UIStyleSheet.TextPrimary;
        tx.fontStyle = FontStyle.Bold;
        tx.raycastTarget = false;
    }

    private void OpenOptions()
    {
        if (_canvas == null)
            return;

        if (_optionsRoot != null)
        {
            var active = !_optionsRoot.activeSelf;
            _optionsRoot.SetActive(active);
            return;
        }

        _optionsRoot = new GameObject("OptionsOverlay", typeof(RectTransform), typeof(Image));
        _optionsRoot.transform.SetParent(_canvas.transform, false);
        var rootRt = _optionsRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;
        var dim = _optionsRoot.GetComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0.04f, 0.03f, 0.08f, 0.82f);
        dim.raycastTarget = true;

        var panel = new GameObject("OptionsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(_optionsRoot.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(520f, 320f);
        prt.anchoredPosition = Vector2.zero;
        var pimg = panel.GetComponent<Image>();
        UIStyleSheet.ApplyPanelImage(pimg, 0.96f);

        var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
        title.transform.SetParent(panel.transform, false);
        var trt = title.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.08f, 0.72f);
        trt.anchorMax = new Vector2(0.92f, 0.94f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var ttx = title.GetComponent<Text>();
        ttx.text = "Options";
        UIStyleSheet.ApplyPrimaryTitle(ttx, 28);

        var body = new GameObject("Body", typeof(RectTransform), typeof(Text));
        body.transform.SetParent(panel.transform, false);
        var brt = body.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.1f, 0.28f);
        brt.anchorMax = new Vector2(0.9f, 0.68f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        var btx = body.GetComponent<Text>();
        btx.text = "Audio and display settings will appear here in a future update.\n\nTap Close to return.";
        UIStyleSheet.ApplyBodyText(btx, 18);
        btx.alignment = TextAnchor.MiddleCenter;

        var closeGo = new GameObject("CloseOptionsButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(panel.transform, false);
        var cRt = closeGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.06f);
        cRt.anchorMax = new Vector2(0.5f, 0.06f);
        cRt.pivot = new Vector2(0.5f, 0f);
        cRt.sizeDelta = new Vector2(200f, 48f);
        cRt.anchoredPosition = Vector2.zero;
        var cImg = closeGo.GetComponent<Image>();
        UIStyleSheet.ApplyButtonGraphic(cImg, UIStyleSheet.ButtonNormal);
        var cBtn = closeGo.GetComponent<Button>();
        UIStyleSheet.ApplyButtonStates(cBtn);
        cBtn.onClick.AddListener(() => _optionsRoot.SetActive(false));

        var cCap = new GameObject("Cap", typeof(RectTransform), typeof(Text));
        cCap.transform.SetParent(closeGo.transform, false);
        var ccr = cCap.GetComponent<RectTransform>();
        ccr.anchorMin = Vector2.zero;
        ccr.anchorMax = Vector2.one;
        ccr.offsetMin = new Vector2(4f, 2f);
        ccr.offsetMax = new Vector2(-4f, -2f);
        var ctx = cCap.GetComponent<Text>();
        ctx.text = "Close";
        ctx.alignment = TextAnchor.MiddleCenter;
        ctx.font = UIStyleSheet.GetUiFont();
        ctx.fontSize = 20;
        ctx.color = UIStyleSheet.TextPrimary;
        ctx.raycastTarget = false;

        closeGo.transform.SetAsLastSibling();
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
