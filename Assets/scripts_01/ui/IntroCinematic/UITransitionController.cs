using System.Collections;
using UnityEngine;

/// <summary>
/// Transiciones suaves con <see cref="CanvasGroup"/> (tiempo no escalado, adecuado entre escenas / UI).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class UITransitionController : MonoBehaviour
{
    private CanvasGroup _cg;
    private Coroutine _running;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        _cg.blocksRaycasts = false;
        _cg.interactable = false;
    }

    /// <summary>Fija alpha inmediatamente y cancela transición en curso.</summary>
    public void SnapAlpha(float alpha)
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        _cg.alpha = Mathf.Clamp01(alpha);
    }

    public Coroutine FadeTo(float targetAlpha, float durationSeconds, bool blockRaycastsWhileVisible = false)
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        _running = StartCoroutine(FadeRoutine(Mathf.Clamp01(targetAlpha), Mathf.Max(0.01f, durationSeconds), blockRaycastsWhileVisible));
        return _running;
    }

    public IEnumerator FadeToCoroutine(float targetAlpha, float durationSeconds, bool blockRaycastsWhileVisible = false)
    {
        yield return FadeRoutine(Mathf.Clamp01(targetAlpha), Mathf.Max(0.01f, durationSeconds), blockRaycastsWhileVisible);
    }

    private IEnumerator FadeRoutine(float target, float duration, bool blockWhenOpaque)
    {
        var start = _cg.alpha;
        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var k = Mathf.Clamp01(t / duration);
            _cg.alpha = Mathf.Lerp(start, target, k);
            var opaque = _cg.alpha > 0.02f;
            _cg.blocksRaycasts = blockWhenOpaque && opaque;
            yield return null;
        }

        _cg.alpha = target;
        _cg.blocksRaycasts = blockWhenOpaque && _cg.alpha > 0.02f;
        _running = null;
    }
}
