using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Panel de pausa que aparece al presionar ESC durante el juego.
/// Botones: Continuar, Reiniciar, Menu Principal.
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    private GameObject _overlay;
    private bool _didStyle;

    private void OnEnable()
    {
        GameManager.OnGamePaused  += OnGamePaused;
        GameManager.OnGameResumed += OnGameResumed;
    }

    private void OnDisable()
    {
        GameManager.OnGamePaused  -= OnGamePaused;
        GameManager.OnGameResumed -= OnGameResumed;
    }

    private void Start()
    {
        if (panelRoot != null)
        {
            EnsureOverlay();
            StylePanel();
            panelRoot.SetActive(false);
            if (_overlay != null) _overlay.SetActive(false);
        }
    }

    private void EnsureOverlay()
    {
        if (_overlay != null || panelRoot == null)
            return;
        var parent = panelRoot.transform.parent;
        if (parent == null)
            return;
        _overlay = UIStyleSheet.CreateOverlayDim(parent);
        // Clic en overlay = NO hace nada (strict close flow)
        var btn = _overlay.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        // Overlay debe ir justo antes del panel en el hierarchy
        _overlay.transform.SetSiblingIndex(panelRoot.transform.GetSiblingIndex());
    }

    private void StylePanel()
    {
        if (_didStyle || panelRoot == null)
            return;
        _didStyle = true;
        UIStyleSheet.StylePauseMenuRoot(panelRoot);
    }

    private void OnGamePaused()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;
        // Solo mostrar si la pausa viene de ESC (no de la tienda)
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            EnsureOverlay();
            StylePanel();
            if (_overlay != null) _overlay.SetActive(true);
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                var rt = panelRoot.GetComponent<RectTransform>();
                if (rt != null)
                    StartCoroutine(UIStyleSheet.AnimatePanelScale(rt, 0.12f));
            }
        }
    }

    private void OnGameResumed()
    {
        panelRoot?.SetActive(false);
        if (_overlay != null) _overlay.SetActive(false);
    }

    public void Resume()
    {
        GameManager.Instance?.ResumeFromPause();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}

