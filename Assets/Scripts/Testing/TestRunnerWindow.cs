#if UNITY_EDITOR
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
}
#endif