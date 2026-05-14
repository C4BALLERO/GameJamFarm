using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mensajes breves en pantalla (toasts) para interacciones: granero, errores genéricos, etc.
/// Se crea bajo el Canvas la primera vez que se usa.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionFeedbackUI : MonoBehaviour
{
    private const string RootName = "InteractionFeedbackRoot";

    private static InteractionFeedbackUI _instance;
    private Text _label;
    private CanvasGroup _group;
    private Coroutine _fadeCo;

    public static void Show(string message, Color color, float seconds = 1.35f)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        Ensure();
        _instance.DoShow(message, color, seconds);
    }

    private static void Ensure()
    {
        if (_instance != null)
            return;

        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            Debug.LogWarning("[InteractionFeedbackUI] No Canvas named 'Canvas'.");
            return;
        }

        var existing = canvasGo.transform.Find(RootName);
        if (existing != null)
        {
            _instance = existing.GetComponent<InteractionFeedbackUI>();
            if (_instance != null)
                return;
        }

        var root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(canvasGo.transform, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.12f, 0.08f);
        rt.anchorMax = new Vector2(0.88f, 0.14f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var bg = root.GetComponent<Image>();
        UIStyleSheet.ApplySolidPanelTint(bg, new Color(0.06f, 0.05f, 0.12f, 0.82f));
        bg.raycastTarget = false;
        var group = root.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.alpha = 0f;

        var txGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        txGo.transform.SetParent(root.transform, false);
        var trt = txGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 4f);
        trt.offsetMax = new Vector2(-10f, -4f);
        var tx = txGo.GetComponent<Text>();
        tx.alignment = TextAnchor.MiddleCenter;
        tx.font = UIStyleSheet.GetUiFont();
        tx.fontSize = 15;
        tx.supportRichText = true;

        _instance = root.AddComponent<InteractionFeedbackUI>();
        _instance._label = tx;
        _instance._group = group;
    }

    private void DoShow(string message, Color color, float seconds)
    {
        if (_label == null || _group == null)
            return;
        _label.text = message;
        _label.color = color;
        transform.SetAsLastSibling();
        if (_fadeCo != null)
            StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(seconds));
    }

    private IEnumerator FadeRoutine(float hold)
    {
        _group.alpha = 0f;
        var t = 0f;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Clamp01(t / 0.12f);
            yield return null;
        }

        _group.alpha = 1f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, hold));

        t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = 1f - Mathf.Clamp01(t / 0.35f);
            yield return null;
        }

        _group.alpha = 0f;
        _label.text = string.Empty;
        _fadeCo = null;
    }
}
