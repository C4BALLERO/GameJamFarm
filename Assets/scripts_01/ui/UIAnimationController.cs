using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles smooth entrance and exit animations for UI panels and tabs.
/// Uses unscaled delta time to animate even when Time.timeScale is 0.
/// </summary>
public static class UIAnimationController
{
    public static IEnumerator FadeInAndScale(RectTransform panel, CanvasGroup group, float duration = 0.15f)
    {
        if (panel == null) yield break;
        
        if (group != null)
            group.alpha = 0f;
        
        panel.localScale = Vector3.one * 0.9f;
        var elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var smooth = Mathf.SmoothStep(0f, 1f, t);
            
            panel.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, smooth);
            if (group != null)
                group.alpha = smooth;
                
            yield return null;
        }
        
        panel.localScale = Vector3.one;
        if (group != null)
            group.alpha = 1f;
    }
}
