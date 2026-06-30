using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Combinations.Tests
{
    public class TestRunnerWindow : EditorWindow
    {
        private string _registrationFilePath = "Assets/Tests/test_registration.json";
        private string _outputDirectory = "TestResults";
        private List<string> _selectedTags = new List<string>();

        private bool _isRunning = false;
        private Vector2 _scrollPosition;

        private bool _tagSmoke = false;
        private bool _tagFull = false;
        private bool _tagCritical = false;
        private bool _tagNightly = false;

        [MenuItem("Tests/Test Runner Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<TestRunnerWindow>("Test Runner");
            window.minSize = new Vector2(450, 600);
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("Unity Test Runner", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Настройки путей
            GUILayout.Label("Configuration", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _registrationFilePath = EditorGUILayout.TextField("Registration File:", _registrationFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select Registration File", "Assets/Tests", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    _registrationFilePath = FileUtil.GetProjectRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            _outputDirectory = EditorGUILayout.TextField("Output Directory:", _outputDirectory);

            EditorGUILayout.Space();

            // Фильтр по тегам
            GUILayout.Label("Tag Filters", EditorStyles.boldLabel);
            _tagSmoke = EditorGUILayout.Toggle("Smoke Tests", _tagSmoke);
            _tagFull = EditorGUILayout.Toggle("Full Tests", _tagFull);
            _tagCritical = EditorGUILayout.Toggle("Critical Tests", _tagCritical);
            _tagNightly = EditorGUILayout.Toggle("Nightly Tests", _tagNightly);

            EditorGUILayout.Space();

            // Проверка файла регистрации
            if (File.Exists(_registrationFilePath))
            {
                EditorGUILayout.HelpBox($"✓ Registration file found: {_registrationFilePath}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"✗ Registration file not found: {_registrationFilePath}", MessageType.Error);
            }

            EditorGUILayout.Space();

            // Кнопки запуска
            GUI.enabled = !_isRunning && File.Exists(_registrationFilePath);

            if (GUILayout.Button("Run All Tests", GUILayout.Height(40)))
            {
                RunTests(null);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Selected Tags", GUILayout.Height(30)))
            {
                var tags = GetSelectedTags();
                if (tags.Count > 0)
                {
                    RunTests(tags);
                }
                else
                {
                    EditorUtility.DisplayDialog("No Tags Selected", "Please select at least one tag filter.", "OK");
                }
            }

            if (GUILayout.Button("Run Smoke Only", GUILayout.Height(30)))
            {
                RunTests(new List<string> { "smoke" });
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            EditorGUILayout.Space();

            // Кнопки открытия результатов
            GUILayout.Label("Results", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Passed"))
            {
                OpenFolder(Path.Combine(_outputDirectory, "passed"));
            }
            if (GUILayout.Button("Open Failed"))
            {
                OpenFolder(Path.Combine(_outputDirectory, "failed"));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Summary"))
            {
                OpenFolder(Path.Combine(_outputDirectory, "summary"));
            }
            if (GUILayout.Button("Open All Results"))
            {
                OpenFolder(_outputDirectory);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clear All Results", GUILayout.Height(25)))
            {
                ClearResults();
            }

            EditorGUILayout.Space();

            // Статус
            if (_isRunning)
            {
                EditorGUILayout.HelpBox("Tests are running... Check console for progress.", MessageType.Warning);
            }

            // Информация о структуре
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Results Structure:\n" +
                $"• {_outputDirectory}/passed/ - Successful tests\n" +
                $"• {_outputDirectory}/failed/ - Failed tests (full details)\n" +
                $"• {_outputDirectory}/summary/ - Summary reports (only failed tests, no test cases)",
                MessageType.Info
            );

            EditorGUILayout.EndScrollView();
        }

        private List<string> GetSelectedTags()
        {
            var tags = new List<string>();

            if (_tagSmoke) tags.Add("smoke");
            if (_tagFull) tags.Add("full");
            if (_tagCritical) tags.Add("critical");
            if (_tagNightly) tags.Add("nightly");

            return tags;
        }

        private async void RunTests(List<string> tags)
        {
            _isRunning = true;

            try
            {
                Debug.Log("=== Starting Tests ===");
                if (tags != null && tags.Count > 0)
                {
                    Debug.Log($"Tag filters: {string.Join(", ", tags)}");
                }

                var masterTester = new MasterTester(
                    registrationFilePath: _registrationFilePath,
                    outputDirectory: _outputDirectory,
                    tagsFilter: tags
                );

                bool success = await masterTester.RunAllTests();

                if (success)
                {
                    Debug.Log("✅ All tests passed!");
                    EditorUtility.DisplayDialog("Tests Complete",
                        $"All tests passed!\n\nResults saved to:\n{_outputDirectory}/passed/", "OK");
                }
                else
                {
                    Debug.LogError("❌ Some tests failed!");
                    EditorUtility.DisplayDialog("Tests Complete",
                        $"Some tests failed!\n\nFailed tests in:\n{_outputDirectory}/failed/\n\nSummary in:\n{_outputDirectory}/summary/", "OK");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Test execution error: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Test Error", $"Error running tests: {ex.Message}", "OK");
            }
            finally
            {
                _isRunning = false;
                Repaint();
            }
        }

        private void OpenFolder(string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", relativePath);

            if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Folder Not Found",
                    $"Folder '{relativePath}' does not exist yet.\nRun tests first.", "OK");
            }
        }

        private void ClearResults()
        {
            if (EditorUtility.DisplayDialog("Clear Test Results",
                "Are you sure you want to delete all test results?",
                "Yes", "No"))
            {
                string fullPath = Path.Combine(Application.dataPath, "..", _outputDirectory);

                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                    Debug.Log("Test results cleared.");
                    EditorUtility.DisplayDialog("Clear Complete", "All test results have been deleted.", "OK");
                }
            }
        }
    }
}