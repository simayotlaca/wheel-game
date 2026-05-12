#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AndroidApkBuilder
{
    private const string OutputDir = "Build";
    private const string ApkName = "VertigoWheel.apk";

    [MenuItem("Tools/Build/Android APK")]
    public static void BuildApk() => Run(launchAfter: false);

    [MenuItem("Tools/Build/Android APK + Run")]
    public static void BuildApkAndRun() => Run(launchAfter: true);

    private static void Run(bool launchAfter)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
        {
            Debug.LogError("[AndroidApkBuilder] Stop Play mode before building.");
            return;
        }

        if (System.Type.GetType("UILayoutBuilder") != null)
        {
            try { UILayoutBuilder.ApplyFinalLayout(); }
            catch (System.Exception e) { Debug.LogWarning("[AndroidApkBuilder] UILayoutBuilder pass skipped: " + e.Message); }
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[AndroidApkBuilder] Switching platform to Android…");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[AndroidApkBuilder] Platform switch to Android failed. Install the Android Build Support module via Unity Hub and try again.");
                return;
            }
        }

        string[] scenes = ResolveScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[AndroidApkBuilder] No scenes resolved. Open the wheel scene OR add one to File → Build Settings → Scenes In Build.");
            return;
        }

        Directory.CreateDirectory(OutputDir);
        string outPath = Path.Combine(OutputDir, ApkName);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = launchAfter ? BuildOptions.AutoRunPlayer : BuildOptions.None
        };

        Debug.Log($"[AndroidApkBuilder] Building {scenes.Length} scene(s) → {outPath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            string sizeMb = (report.summary.totalSize / (1024f * 1024f)).ToString("0.0");
            Debug.Log($"[AndroidApkBuilder] ✅ Build succeeded — {sizeMb} MB → {outPath}");
            EditorUtility.RevealInFinder(outPath);
        }
        else
        {
            Debug.LogError($"[AndroidApkBuilder] ❌ Build {report.summary.result} ({report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings)");
        }
    }

    private static string[] ResolveScenes()
    {
        var enabled = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path)) enabled.Add(s.path);
        }
        if (enabled.Count > 0) return enabled.ToArray();

        var active = EditorSceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(active.path)) return new[] { active.path };
        return System.Array.Empty<string>();
    }
}
#endif
