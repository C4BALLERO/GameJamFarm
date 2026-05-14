using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Un panel de historia: ilustración, texto y botón «Siguiente» (esquina inferior izquierda).
/// </summary>
[DisallowMultipleComponent]
public sealed class DialoguePanelController : MonoBehaviour
{
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Image illustration;
    [SerializeField] private Text bodyText;
    [SerializeField] private Button siguienteButton;
    [SerializeField] private Text siguienteLabel;

    [Header("Typewriter")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charactersPerSecond = 42f;

    private Coroutine _typeCo;
    private bool _advanceRequested;
    private string _pendingFullText = string.Empty;

    public event Action SiguienteClicked;

    /// <summary>Usado cuando la jerarquía se genera en runtime (p. ej. <see cref="StorySequenceController"/>).</summary>
    public void RuntimeWire(CanvasGroup root, Image art, Text body, Button next, Text nextCaption)
    {
        panelGroup = root;
        illustration = art;
        bodyText = body;
        siguienteButton = next;
        siguienteLabel = nextCaption;
    }

    private void Awake()
    {
        if (siguienteButton != null)
            siguienteButton.onClick.AddListener(OnSiguiente);

        if (siguienteLabel != null)
            siguienteLabel.text = "Siguiente";

        AddHoverPulse(siguienteButton);
    }

    private void OnDestroy()
    {
        if (siguienteButton != null)
            siguienteButton.onClick.RemoveListener(OnSiguiente);
    }

    public void SetInteractable(bool value)
    {
        if (panelGroup != null)
            panelGroup.interactable = value;
        if (siguienteButton != null)
            siguienteButton.interactable = value;
    }

    /// <summary>Último paso: mostrar «Iniciar» en lugar de «Siguiente» (opcional).</summary>
    public void SetSiguienteCaption(string caption)
    {
        if (siguienteLabel != null)
            siguienteLabel.text = string.IsNullOrEmpty(caption) ? "Siguiente" : caption;
    }

    public void Configure(Sprite art, string fullMessage)
    {
        if (illustration != null)
        {
            illustration.sprite = art;
            illustration.enabled = art != null;
            illustration.preserveAspect = true;
        }

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
            if (_typeCo != null)
            {
                StopCoroutine(_typeCo);
                _typeCo = null;
            }

            _pendingFullText = fullMessage ?? string.Empty;
            if (useTypewriter && !string.IsNullOrEmpty(_pendingFullText))
                _typeCo = StartCoroutine(Typewriter(_pendingFullText));
            else if (bodyText != null)
                bodyText.text = _pendingFullText;
        }
    }

    public IEnumerator ShowFadeIn(float duration)
    {
        if (panelGroup == null)
            yield break;
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            panelGroup.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        // Sin esto, GraphicRaycaster ignora los hijos y el botón «Siguiente» no recibe clics.
        panelGroup.blocksRaycasts = true;
    }

    public IEnumerator HideFadeOut(float duration)
    {
        if (panelGroup == null)
            yield break;
        var start = panelGroup.alpha;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var k = Mathf.Clamp01(t / duration);
            panelGroup.alpha = Mathf.Lerp(start, 0f, k);
            yield return null;
        }

        panelGroup.alpha = 0f;
    }

    /// <summary>Bloquea hasta que el jugador pulse Siguiente (o Escape como atajo).</summary>
    public IEnumerator WaitForAdvanceUnscaled()
    {
        _advanceRequested = false;
        while (!_advanceRequested)
        {
            if (TryKeyboardAdvanceShortcut())
                _advanceRequested = true;
            yield return null;
        }

        _advanceRequested = false;
    }

    private static bool TryKeyboardAdvanceShortcut()
    {
        var down = false;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            down = k.escapeKey.wasPressedThisFrame ||
                   k.enterKey.wasPressedThisFrame ||
                   k.numpadEnterKey.wasPressedThisFrame ||
                   k.spaceKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        down |= Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        return down;
    }

    private void OnSiguiente()
    {
        if (_typeCo != null && bodyText != null && !string.IsNullOrEmpty(_pendingFullText))
        {
            StopCoroutine(_typeCo);
            _typeCo = null;
            bodyText.text = _pendingFullText;
        }

        _advanceRequested = true;
        SiguienteClicked?.Invoke();
    }

    public bool IsTyping => _typeCo != null;

    private IEnumerator Typewriter(string full)
    {
        if (bodyText == null)
            yield break;
        var sb = new StringBuilder();
        var delay = 1f / Mathf.Max(4f, charactersPerSecond);
        foreach (var ch in full)
        {
            sb.Append(ch);
            bodyText.text = sb.ToString();
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    private static void AddHoverPulse(Button btn)
    {
        if (btn == null)
            return;
        var pulse = btn.gameObject.GetComponent<SiguienteHoverPulse>();
        if (pulse == null)
            pulse = btn.gameObject.AddComponent<SiguienteHoverPulse>();
        pulse.Bind(btn);
    }

    /// <summary>Animación sutil al pasar el ratón por «Siguiente».</summary>
    private sealed class SiguienteHoverPulse : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Button _btn;
        private Vector3 _baseScale = Vector3.one;
        private bool _hover;

        public void Bind(Button b)
        {
            _btn = b;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            if (_btn == null)
                return;
            var target = _hover ? 1.06f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * target, Time.unscaledDeltaTime * 10f);
        }

        public void OnPointerEnter(PointerEventData eventData) => _hover = true;

        public void OnPointerExit(PointerEventData eventData) => _hover = false;
    }
}
