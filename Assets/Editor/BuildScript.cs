using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Linq;

public static class BuildScript
{
    private static readonly string[] Scenes = EditorBuildSettings.scenes
        .Where(s => s.enabled)
        .Select(s => s.path)
        .ToArray();

    [MenuItem("Build/Build Windows Client")]
    public static void BuildWindowsClient()
    {
        string path = "D:/Builds/ClientBuild/ClientBuild.exe";
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        RunBuild(options, "Windows Client");
    }

    [MenuItem("Build/Build Linux Server")]
    public static void BuildLinuxServer()
    {
        string path = "D:/Builds/ServerBuild/ServerBuild.x86_64";
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.EnableHeadlessMode
        };
        RunBuild(options, "Linux Server");
    }

    [MenuItem("Build/Build All")]
    public static void BuildAll()
    {
        BuildWindowsClient();
        BuildLinuxServer();
    }

    public static void BuildWindowsClientCLI()
    {
        string path = "D:/Builds/ClientBuild/ClientBuild.exe";
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        RunBuild(options, "Windows Client");
    }

    public static void BuildLinuxServerCLI()
    {
        string path = "D:/Builds/ServerBuild/ServerBuild.x86_64";
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.EnableHeadlessMode
        };
        RunBuild(options, "Linux Server");
    }

    public static void BuildAllCLI()
    {
        BuildWindowsClientCLI();
        BuildLinuxServerCLI();
    }

    private static void RunBuild(BuildPlayerOptions options, string label)
    {
        Debug.Log($"[BuildScript] Starting {label} build...");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] {label} build succeeded: {summary.totalSize} bytes, {summary.totalTime}");
        }
        else
        {
            Debug.LogError($"[BuildScript] {label} build failed: {summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
