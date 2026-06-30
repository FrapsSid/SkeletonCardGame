using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class CITestRunner
{
    /// <summary>
    /// Метод для запуска из командной строки Unity
    /// </summary>
    public static async void RunTests()
    {
        try
        {
            // Получение аргументов командной строки
            string[] args = Environment.GetCommandLineArgs();

            string registrationFile = GetArgument(args, "-testRegistration", "Assets/Tests/test_registration.json");
            string outputDir = GetArgument(args, "-outputDir", "TestResults");
            string tagsArg = GetArgument(args, "-tags", "");

            List<string> tags = string.IsNullOrEmpty(tagsArg)
                ? new List<string>()
                : tagsArg.Split(',').ToList();

            Debug.Log($"Starting tests with registration file: {registrationFile}");
            Debug.Log($"Output directory: {outputDir}");
            Debug.Log($"Tags filter: {string.Join(", ", tags)}");

            // Создание и запуск master tester
            var masterTester = new MasterTester(registrationFile, outputDir, tags);
            bool success = await masterTester.RunAllTests();

            // Завершение с кодом выхода
            EditorApplication.Exit(success ? 0 : 1);
        }
        catch (Exception ex)
        {
            Debug.LogError($"CI Test Runner failed: {ex.Message}\n{ex.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    private static string GetArgument(string[] args, string name, string defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }
        return defaultValue;
    }
}