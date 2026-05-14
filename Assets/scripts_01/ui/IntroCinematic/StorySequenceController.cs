using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Tres viñetas narrativos: fade, texto, «Siguiente»; al terminar la tercera carga el gameplay.
/// Flujo del juego: Menú → esta escena → <see cref="SceneFlowManager.Gameplay"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class StorySequenceController : MonoBehaviour
{
    [SerializeField] private StoryPanelManager panelManager;
    [SerializeField] private string nextSceneAfterStory = SceneFlowManager.Gameplay;
    [SerializeField] private int nextSceneBuildIndex = -1;

    [Header("Timing (unscaled)")]
    [SerializeField] private float entryFadeFromBlackDuration = 1f;
    [SerializeField] private float panelFadeInDuration = 0.85f;
    [SerializeField] private float panelFadeOutDuration = 0.55f;
    [SerializeField] private float betweenPanelsBlackAlpha = 0.38f;
    [SerializeField] private float betweenPanelsFadeIn = 0.14f;
    [SerializeField] private float betweenPanelsFadeOut = 0.22f;
    [SerializeField] private float exitFadeToBlackDuration = 0.95f;

    [Header("Optional ambience")]
    [SerializeField] private bool enableAmbientStoryMotes = true;
    [SerializeField] private int ambientMoteCount = 14;

    [Header("Layout")]
    [Tooltip("Si es false: cada beat usa panel1/2/3 a pantalla casi completa + texto en franja inferior.")]
    [SerializeField] private bool useOrnateFrameLayout;
    [Tooltip("Ruta Resources (sin extensión) del marco con pergamino; solo si useOrnateFrameLayout.")]
    [SerializeField] private string storyFrameResourcePath = "story_panel_frame";
    [SerializeField] private Vector2 parchmentAnchorMin = new Vector2(0.28f, 0.36f);
    [SerializeField] private Vector2 parchmentAnchorMax = new Vector2(0.72f, 0.62f);
    [SerializeField] private int parchmentFontSize = 19;
    [SerializeField] private Color parchmentTextColor = new Color32(42, 30, 22, 255);
    [Header("Layout viñetas (panel1/2/3)")]
    [SerializeField] private Vector2 fullBleedTextAnchorMin = new Vector2(0.06f, 0.12f);
    [SerializeField] private Vector2 fullBleedTextAnchorMax = new Vector2(0.96f, 0.36f);
    [SerializeField] private int fullBleedFontSize = 20;
    [SerializeField] private bool fullBleedReadabilityPlate = true;

    private UITransitionController _fadeOverlay;
    private DialoguePanelController[] _panels = System.Array.Empty<DialoguePanelController>();

    private void Start()
    {
        EnsureEventSystem();
        if (panelManager == null)
            panelManager = GetComponent<StoryPanelManager>();

        BuildStoryUi();
        StartCoroutine(RunStory());
    }

    private IEnumerator RunStory()
    {
        var beats = panelManager != null ? panelManager.Beats : StoryPanelManager.CreateTemplateBeats();
        if (beats == null || beats.Length == 0)
            beats = StoryPanelManager.CreateTemplateBeats();

        if (_panels.Length != beats.Length)
        {
            Debug.LogError("[StorySequenceController] Panel count mismatch.");
            yield break;
        }

        var ornateFrame = string.IsNullOrWhiteSpace(storyFrameResourcePath)
            ? null
            : LoadIllustration(storyFrameResourcePath);

        _fadeOverlay.SnapAlpha(1f);
        yield return _fadeOverlay.FadeToCoroutine(0f, Mathf.Max(0.05f, entryFadeFromBlackDuration), false);

        foreach (var p in _panels)
            p.gameObject.SetActive(false);

        for (var i = 0; i < beats.Length; i++)
        {
            var beat = beats[i];
            var panel = _panels[i];
            panel.gameObject.SetActive(true);

            var beatArt = LoadIllustration(beat.illustrationResourcePath);
            Sprite mainVisual;
            if (useOrnateFrameLayout)
                mainVisual = ornateFrame != null ? ornateFrame : beatArt;
            else
                mainVisual = beatArt != null ? beatArt : ornateFrame;

            panel.Configure(mainVisual, beat.narrative ?? string.Empty);
            panel.SetInteractable(true);

            yield return panel.ShowFadeIn(Mathf.Max(0.05f, panelFadeInDuration));
            yield return panel.WaitForAdvanceUnscaled();
            yield return panel.HideFadeOut(Mathf.Max(0.05f, panelFadeOutDuration));
            panel.SetInteractable(false);
            panel.gameObject.SetActive(false);

            if (i < beats.Length - 1)
            {
                yield return _fadeOverlay.FadeToCoroutine(
                    Mathf.Clamp01(betweenPanelsBlackAlpha),
                    Mathf.Max(0.02f, betweenPanelsFadeIn),
                    true);
                yield return _fadeOverlay.FadeToCoroutine(0f, Mathf.Max(0.02f, betweenPanelsFadeOut), false);
            }
        }

        yield return _fadeOverlay.FadeToCoroutine(1f, Mathf.Max(0.05f, exitFadeToBlackDuration), true);

        LoadGameplayScene();
    }

    private void LoadGameplayScene()
    {
        if (_fadeOverlay != null)
        {
            var cg = _fadeOverlay.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.blocksRaycasts = false;
        }

        if (nextSceneBuildIndex >= 0 && nextSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneBuildIndex, LoadSceneMode.Single);
            return;
        }

        var name = string.IsNullOrWhiteSpace(nextSceneAfterStory)
            ? SceneFlowManager.Gameplay
            : nextSceneAfterStory.Trim();

        if (Application.CanStreamedLevelBeLoaded(name))
        {
            SceneManager.LoadScene(name, LoadSceneMode.Single);
            return;
        }

        Debug.LogError("[StorySequenceController] La escena '" + name + "' no está en Build Settings. Cargando fallback: " + SceneFlowManager.Gameplay);
        if (Application.CanStreamedLevelBeLoaded(SceneFlowManager.Gameplay))
            SceneManager.LoadScene(SceneFlowManager.Gameplay, LoadSceneMode.Single);
    }

    private void BuildStoryUi()
    {
        var canvasGo = new GameObject("IntroStoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

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

        var beats = panelManager != null ? panelManager.Beats : StoryPanelManager.CreateTemplateBeats();
        if (beats == null || beats.Length == 0)
            beats = StoryPanelManager.CreateTemplateBeats();

        _panels = new DialoguePanelController[beats.Length];
        for (var i = 0; i < beats.Length; i++)
        {
            var idx = i + 1;
            var panelRoot = new GameObject("StoryPanel_0" + idx, typeof(RectTransform), typeof(CanvasGroup));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRt = panelRoot.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var dim = CreateUiImage(panelRoot.transform, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dim.sprite = null;
            dim.color = useOrnateFrameLayout
                ? new Color(0.02f, 0.02f, 0.04f, 0.42f)
                : new Color(0.02f, 0.02f, 0.05f, 0.55f);
            dim.raycastTarget = false;

            var board = CreateUiImage(panelRoot.transform, "StoryVisual", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);
            board.raycastTarget = false;
            board.preserveAspect = true;
            board.type = Image.Type.Simple;
            board.color = Color.white;

            if (!useOrnateFrameLayout && fullBleedReadabilityPlate)
            {
                var plate = CreateUiImage(panelRoot.transform, "TextReadabilityPlate",
                    new Vector2(0f, 0f), new Vector2(1f, 0.4f), Vector2.zero, Vector2.zero);
                plate.sprite = null;
                plate.color = new Color(0.04f, 0.03f, 0.08f, 0.72f);
                plate.raycastTarget = false;
            }

            var bodyGo = new GameObject("StoryText", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(panelRoot.transform, false);
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            if (useOrnateFrameLayout)
            {
                bodyRt.anchorMin = parchmentAnchorMin;
                bodyRt.anchorMax = parchmentAnchorMax;
                bodyRt.offsetMin = new Vector2(18f, 12f);
                bodyRt.offsetMax = new Vector2(-18f, -12f);
            }
            else
            {
                bodyRt.anchorMin = fullBleedTextAnchorMin;
                bodyRt.anchorMax = fullBleedTextAnchorMax;
                bodyRt.offsetMin = new Vector2(16f, 10f);
                bodyRt.offsetMax = new Vector2(-16f, -10f);
            }

            var bodyText = bodyGo.GetComponent<Text>();
            bodyText.font = UIStyleSheet.GetUiFont();
            bodyText.fontSize = useOrnateFrameLayout ? parchmentFontSize : fullBleedFontSize;
            bodyText.color = useOrnateFrameLayout ? parchmentTextColor : UIStyleSheet.TextPrimary;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.supportRichText = true;
            bodyText.raycastTarget = false;

            var btnGo = new GameObject("SiguienteButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panelRoot.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(0f, 0f);
            btnRt.pivot = new Vector2(0f, 0f);
            btnRt.sizeDelta = new Vector2(236f, 58f);
            btnRt.anchoredPosition = new Vector2(48f, 44f);
            var btnImg = btnGo.GetComponent<Image>();
            UIStyleSheet.ApplyButtonGraphic(btnImg, UIStyleSheet.ButtonNormal);
            var btn = btnGo.GetComponent<Button>();
            UIStyleSheet.ApplyButtonStates(btn);

            var capGo = new GameObject("Caption", typeof(RectTransform), typeof(Text));
            capGo.transform.SetParent(btnGo.transform, false);
            var capRt = capGo.GetComponent<RectTransform>();
            capRt.anchorMin = Vector2.zero;
            capRt.anchorMax = Vector2.one;
            capRt.offsetMin = new Vector2(8f, 4f);
            capRt.offsetMax = new Vector2(-8f, -4f);
            var capText = capGo.GetComponent<Text>();
            capText.text = "Siguiente";
            capText.alignment = TextAnchor.MiddleCenter;
            capText.font = UIStyleSheet.GetUiFont();
            capText.fontSize = 22;
            capText.color = UIStyleSheet.TextPrimary;
            capText.fontStyle = FontStyle.Bold;
            capText.raycastTarget = false;

            btnGo.transform.SetAsLastSibling();

            var panelCg = panelRoot.GetComponent<CanvasGroup>();
            panelCg.alpha = 0f;
            panelCg.blocksRaycasts = false;
            panelCg.interactable = false;

            panelRoot.SetActive(false);
            var dlg = panelRoot.AddComponent<DialoguePanelController>();
            dlg.RuntimeWire(panelCg, board, bodyText, btn, capText);
            panelRoot.SetActive(true);

            _panels[i] = dlg;
        }

        if (enableAmbientStoryMotes && ambientMoteCount > 0)
        {
            var motesHost = new GameObject("AmbientMotes", typeof(RectTransform));
            motesHost.transform.SetParent(canvasGo.transform, false);
            var mhRt = motesHost.GetComponent<RectTransform>();
            mhRt.anchorMin = Vector2.zero;
            mhRt.anchorMax = Vector2.one;
            mhRt.offsetMin = mhRt.offsetMax = Vector2.zero;
            motesHost.transform.SetSiblingIndex(0);
            var motes = motesHost.AddComponent<IntroStoryAmbientMotes>();
            motes.Configure(ambientMoteCount);
        }

        var fadeGo = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(UITransitionController));
        fadeGo.transform.SetParent(canvasGo.transform, false);
        var fadeRt = fadeGo.GetComponent<RectTransform>();
        fadeRt.anchorMin = Vector2.zero;
        fadeRt.anchorMax = Vector2.one;
        fadeRt.offsetMin = fadeRt.offsetMax = Vector2.zero;
        var fadeImg = fadeGo.GetComponent<Image>();
        fadeImg.sprite = null;
        fadeImg.color = Color.black;
        fadeImg.raycastTarget = false;
        fadeGo.GetComponent<CanvasGroup>().alpha = 1f;
        _fadeOverlay = fadeGo.GetComponent<UITransitionController>();
    }

    private static Sprite LoadIllustration(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return null;
        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
            return sprite;
        var texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
            return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
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

    private sealed class IntroStoryAmbientMotes : MonoBehaviour
    {
        private sealed class MoteState
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Phase;
            public float Speed;
            public Vector2 BaseAnchored;
        }

        private MoteState[] _states = System.Array.Empty<MoteState>();

        public void Configure(int count)
        {
            count = Mathf.Clamp(count, 1, 48);
            _states = new MoteState[count];
            var parent = (RectTransform)transform;
            for (var i = 0; i < count; i++)
            {
                var go = new GameObject("Mote_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(Random.Range(0.08f, 0.92f), Random.Range(0.1f, 0.9f));
                rt.sizeDelta = new Vector2(Random.Range(2f, 5f), Random.Range(2f, 5f));
                rt.anchoredPosition = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.sprite = UIStyleSheet.GetWhiteUnitSprite();
                img.color = new Color(1f, 0.92f, 0.78f, Random.Range(0.04f, 0.1f));
                img.raycastTarget = false;
                _states[i] = new MoteState
                {
                    Rt = rt,
                    Img = img,
                    Phase = new Vector2(Random.Range(0f, 6.28f), Random.Range(0f, 6.28f)),
                    Speed = Random.Range(0.35f, 0.9f),
                    BaseAnchored = rt.anchoredPosition
                };
            }
        }

        private void Update()
        {
            var t = Time.unscaledTime;
            foreach (var s in _states)
            {
                if (s?.Rt == null)
                    continue;
                var drift = new Vector2(
                    Mathf.Sin(t * s.Speed + s.Phase.x) * 10f,
                    Mathf.Cos(t * (s.Speed * 0.85f) + s.Phase.y) * 8f);
                s.Rt.anchoredPosition = s.BaseAnchored + drift;
                if (s.Img != null)
                {
                    var c = s.Img.color;
                    c.a = Mathf.Lerp(0.035f, 0.11f, 0.5f + 0.5f * Mathf.Sin(t * 1.7f + s.Phase.x));
                    s.Img.color = c;
                }
            }
        }
    }
}
