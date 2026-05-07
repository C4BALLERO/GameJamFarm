using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Panel de pausa que aparece al presionar ESC durante el juego.
/// Botones: Continuar, Reiniciar, Menu Principal.
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

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
            panelRoot.SetActive(false);
    }

    private void OnGamePaused()
    {
        // Solo mostrar si la pausa viene de ESC (no de la tienda)
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            panelRoot?.SetActive(true);
    }

    private void OnGameResumed()
    {
        panelRoot?.SetActive(false);
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
