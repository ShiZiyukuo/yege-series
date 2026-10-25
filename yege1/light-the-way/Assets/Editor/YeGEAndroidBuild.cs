using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 红米 K70 Pro 用的 Android 打包设置与 APK 构建。
/// </summary>
public static class YeGEAndroidBuild
{
    const string PACKAGE_NAME = "com.yege.adventures";

    [MenuItem("YeGE/生成并打包 APK")]
    public static void BuildAllAndPackage()
    {
        // Packaging must never regenerate and overwrite the reviewed adventure scenes.
        AdventureBuilder.BuildAndroid();
    }

    [MenuItem("YeGE/构建 Android APK")]
    public static void BuildApk()
    {
        SetupAndroidSettings();

        string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        string output = System.IO.Path.Combine(projectRoot, "Build", "TheAdventuresOfYeGe.apk");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));

        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.enabled)
            {
                scenes.Add(s.path);
            }
        }
        Debug.Log("[YeGE] 参与打包的场景数: " + scenes.Count);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log("[YeGE] 构建结果: " + summary.result + "，大小 " + summary.totalSize + " 字节，耗时 " + summary.totalTime);
        foreach (BuildStep step in report.steps)
        {
            if (step.messages != null && step.messages.Length > 0)
            {
                foreach (BuildStepMessage msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Warning)
                    {
                        Debug.Log("[YeGE][" + msg.type + "] " + msg.content);
                    }
                }
            }
        }

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[YeGE] APK 输出: " + output);
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("[YeGE] APK 构建失败: " + summary.result);
            EditorApplication.Exit(1);
        }
    }

    /// <summary>Android 玩家设置：横屏、ARM64、包名、安全区</summary>
    public static void SetupAndroidSettings()
    {
        PlayerSettings.companyName = "YeGE";
        PlayerSettings.productName = "The adventures of YeGe";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PACKAGE_NAME);

        // 横屏（红米 K70 Pro）
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.Android.renderOutsideSafeArea = true;
        PlayerSettings.Android.startInFullscreen = true;
        PlayerSettings.Android.androidIsGame = true;

        // 架构：仅 ARM64（K70 Pro 为骁龙 8 Gen 3）
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
        PlayerSettings.Android.useAPKExpansionFiles = false;
        PlayerSettings.Android.forceSDCardPermission = false;
        PlayerSettings.Android.forceInternetPermission = false;

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;

        AssetDatabase.SaveAssets();
        Debug.Log("[YeGE] Android 玩家设置完成");
    }
}
