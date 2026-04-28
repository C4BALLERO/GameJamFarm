using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Hooks the menu start button at runtime so the menu scene survives builds without hand-authored UnityEvents.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuSceneBootstrap : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "scene_00_main";
    [SerializeField] private Button startButton;

    private void Awake()
    {
        if (startButton == null)
            startButton = GetComponentInChildren<Button>(true);

        if (startButton != null)
            startButton.onClick.AddListener(LoadMain);
    }

    private void LoadMain()
    {
        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogError("[MenuSceneBootstrap] Main scene name is empty.");
            return;
        }

        SceneManager.LoadScene(mainSceneName);
    }
}
