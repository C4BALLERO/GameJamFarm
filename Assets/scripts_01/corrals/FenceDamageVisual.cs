using System.Collections;
using UnityEngine;

/// <summary>
/// Feedback visual en segmentos de valla: flash, ligero shake y tinte según porcentaje de vida.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class FenceDamageVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Vector3 _baseScale;
    private Coroutine _shakeRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
    }

    public void OnDamaged(int currentHp, int maxHp)
    {
        if (spriteRenderer == null)
            return;
        var ratio = maxHp <= 0 ? 0f : Mathf.Clamp01((float)currentHp / maxHp);
        var baseCol = spriteRenderer.color;
        var crack = Color.Lerp(new Color(0.42f, 0.32f, 0.28f, baseCol.a), baseCol, ratio);
        spriteRenderer.color = Color.Lerp(crack, baseCol, Mathf.Pow(ratio, 0.55f));

        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeFlash());
    }

    private IEnumerator ShakeFlash()
    {
        const float dur = 0.12f;
        var t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            var wobble = (1f - t / dur) * 0.04f;
            transform.localScale = _baseScale * (1f + Random.Range(-wobble, wobble));
            yield return null;
        }

        transform.localScale = _baseScale;
        _shakeRoutine = null;
    }
}
