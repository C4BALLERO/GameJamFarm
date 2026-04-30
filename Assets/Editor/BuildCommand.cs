#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildCommand
{
    public static void BuildWindows64()
    {
        const string outputPath = "Build/DarkFarmSurvival.exe";

        var enabledScenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (enabledScenes.Length == 0)
        {
            Debug.LogError("[BuildCommand] No enabled scenes in Build Settings.");
            EditorApplication.Exit(2);
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = enabledScenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Debug.Log($"[BuildCommand] Result: {summary.result}, warnings: {summary.totalWarnings}, errors: {summary.totalErrors}, output: {summary.outputPath}");

        if (summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(3);
            return;
        }

        EditorApplication.Exit(0);
    }
}
#endif

