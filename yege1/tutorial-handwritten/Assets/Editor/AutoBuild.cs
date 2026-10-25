using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AutoBuild
{
    public static void BuildAndroidAPK()
    {
        string outputPath = "Build/MyGame.apk";
        System.IO.Directory.CreateDirectory("Build");

        // 基础设置：包名、架构（必须含 ARM64，否则新手机装不上）、横屏
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.lzx.mygame");
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        // 取 Build Settings 里勾选的场景
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        string[] scenes = new string[buildScenes.Length];
        for (int i = 0; i < buildScenes.Length; i++)
        {
            scenes[i] = buildScenes[i].path;
        }
        Debug.Log("AutoBuild: scenes count = " + scenes.Length);
        for (int i = 0; i < scenes.Length; i++)
        {
            Debug.Log("AutoBuild: [" + i + "] " + scenes[i]);
        }

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes;
        options.locationPathName = outputPath;
        options.target = BuildTarget.Android;
        options.options = BuildOptions.None;

        Debug.Log("AutoBuild: start building -> " + outputPath);
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log("AutoBuild: result = " + summary.result + ", size = " + summary.totalSize + " bytes");

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("AutoBuild: SUCCESS");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("AutoBuild: FAILED - " + summary.result);
            EditorApplication.Exit(1);
        }
    }
}
