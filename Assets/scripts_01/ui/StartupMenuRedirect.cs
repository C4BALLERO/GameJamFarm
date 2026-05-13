using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures play mode starts at the menu flow when required player data is missing.
/// </summary>
public static class StartupMenuRedirect
{
    private const string MainSceneName = "scene_00_main";
    private const string MenuSceneName = "scene_01_menu";
    private const string PlayerNamePrefKey = "player_name";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureMenuBeforeGameplay()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != MainSceneName)
            return;

        var hasPlayerName = !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(PlayerNamePrefKey, string.Empty));
        if (hasPlayerName)
            return;

        SceneManager.LoadScene(MenuSceneName);
    }
}
