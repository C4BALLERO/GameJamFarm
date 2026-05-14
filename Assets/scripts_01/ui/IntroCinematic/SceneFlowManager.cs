using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Nombres de escena del flujo intro → menú → historia → gameplay.
/// Centraliza rutas para evitar cadenas mágicas dispersas.
/// </summary>
public static class SceneFlowManager
{
    public const string Splash = "scene_00_splash";
    public const string MainMenu = "scene_01_menu";
    public const string Story = "scene_02_story";
    public const string Gameplay = "scene_00_main";

    public static void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneFlowManager] Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
