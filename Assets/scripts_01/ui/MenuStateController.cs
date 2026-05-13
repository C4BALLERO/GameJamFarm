using UnityEngine;

/// <summary>
/// Controls strict pause behavior when menus are open.
/// Ensures that game only resumes when menus are explicitly closed via X button.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuStateController : MonoBehaviour
{
    public static MenuStateController Instance { get; private set; }

    private int _openMenus = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void RegisterMenuOpen()
    {
        _openMenus++;
        EnforcePauseState();
    }

    public void RegisterMenuClose()
    {
        _openMenus = Mathf.Max(0, _openMenus - 1);
        EnforcePauseState();
    }

    private void EnforcePauseState()
    {
        if (GameManager.Instance == null)
            return;
            
        bool shouldPause = _openMenus > 0;
        // Reutilizamos el sistema seguro de pausa del GameManager
        GameManager.Instance.SetShopPaused(shouldPause);
    }
}
