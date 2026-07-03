#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class TestRunnerWindow : EditorWindow
{
    private string registrationPath = "TestRunner/Tests/test_registration.json";
    private string outputDir = "TestResults";

    [MenuItem("Tests/Test Runner")]
    public static void ShowWindow()
    {
        GetWindow<TestRunnerWindow>("Test Runner");
    }

    public static void RunFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        string registrationPath = GetArgument(args, "--registration", "Assets/Tests/test_registration.json");
        string outputDir = GetArgument(args, "--output", "TestResults");
        string tagsArg = GetArgument(args, "--tags", string.Empty);
        List<string> tags = ParseTags(tagsArg);

        try
        {
            var master = new MasterTester(registrationPath, outputDir, tags);
            bool success = master.RunAllTests().GetAwaiter().GetResult();
            EditorApplication.Exit(success ? 0 : 1);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Test runner failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Master Tester", EditorStyles.boldLabel);

        registrationPath = EditorGUILayout.TextField("Registration File:", registrationPath);
        outputDir = EditorGUILayout.TextField("Output Directory:", outputDir);

        if (GUILayout.Button("Run All Tests"))
        {
            RunTests();
        }
    }

    private async void RunTests()
    {
        Debug.Log("Starting MasterTester...");

        var master = new MasterTester(
            registrationPath,
            outputDir,
            new List<string>()
        );

        bool success = await master.RunAllTests();

        if (success)
            Debug.Log("✅ All tests passed");
        else
            Debug.LogError("❌ Some tests failed");
    }

    private static string GetArgument(string[] args, string name, string defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }

        return defaultValue;
    }

    private static List<string> ParseTags(string tagsArg)
    {
        var tags = new List<string>();
        if (string.IsNullOrWhiteSpace(tagsArg))
            return tags;

        string[] parts = tagsArg.Split(',');
        foreach (string part in parts)
        {
            string tag = part.Trim();
            if (!string.IsNullOrWhiteSpace(tag))
                tags.Add(tag);
        }

        return tags;
    }
}
#endif
