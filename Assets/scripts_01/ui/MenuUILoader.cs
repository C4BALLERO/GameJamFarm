using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hook this to a menu button to load the main gameplay scene by name (<c>scene_00_main</c>).
/// </summary>
public sealed class MenuUILoader : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "scene_00_main";

    public void LoadMainGame()
    {
        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogError("[MenuUILoader] Main scene name is empty.");
            return;
        }

        SceneManager.LoadScene(mainSceneName);
    }
}
