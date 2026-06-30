using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MasterTester
{
    private string _registrationFilePath;
    private string _outputDirectory;
    private List<string> _tagsFilter;

    // Поддиректории для организации результатов
    private string _passedTestsDir;
    private string _failedTestsDir;
    private string _summaryDir;

    public MasterTester(string registrationFilePath, string outputDirectory, List<string> tagsFilter = null)
    {
        _registrationFilePath = registrationFilePath;
        _outputDirectory = outputDirectory;
        _tagsFilter = tagsFilter ?? new List<string>();

        // Инициализация путей к поддиректориям
        _passedTestsDir = Path.Combine(_outputDirectory, "passed");
        _failedTestsDir = Path.Combine(_outputDirectory, "failed");
        _summaryDir = Path.Combine(_outputDirectory, "summary");
    }

    public async Task<bool> RunAllTests()
    {
        try
        {
            TesterFactory.AutoRegisterTesters();
            var registrations = LoadTestRegistrations();
            var testsToRun = FilterTests(registrations);

            Debug.Log($"Running {testsToRun.Count} tests...");

            // Создание директорий
            CreateOutputDirectories();

            bool allTestsPassed = true;
            var allResults = new List<TestResult>();

            foreach (var registration in testsToRun)
            {
                try
                {
                    Debug.Log($"Running test: {registration.testId}");

                    var result = await RunSingleTest(registration);
                    allResults.Add(result);

                    // Сохранение в соответствующую папку
                    SaveTestResult(registration.testId, result);

                    if (!result.success)
                    {
                        allTestsPassed = false;
                        Debug.LogError($"Test {registration.testId} FAILED: {result.message}");
                    }
                    else
                    {
                        Debug.Log($"Test {registration.testId} PASSED");
                    }
                }
                catch (Exception ex)
                {
                    allTestsPassed = false;
                    Debug.LogError($"Error running test {registration.testId}: {ex.Message}");

                    var errorResult = new TestResult
                    {
                        testId = registration.testId,
                        testerId = registration.testerId,
                        success = false,
                        message = $"Exception: {ex.Message}",
                        executionTimeMs = 0
                    };

                    allResults.Add(errorResult);
                    SaveTestResult(registration.testId, errorResult);
                }
            }

            SaveSummaryReport(allResults);

            return allTestsPassed;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Master tester error: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    private void CreateOutputDirectories()
    {
        if (!Directory.Exists(_passedTestsDir))
        {
            Directory.CreateDirectory(_passedTestsDir);
        }

        if (!Directory.Exists(_failedTestsDir))
        {
            Directory.CreateDirectory(_failedTestsDir);
        }

        if (!Directory.Exists(_summaryDir))
        {
            Directory.CreateDirectory(_summaryDir);
        }
    }

    private List<TestRegistration> LoadTestRegistrations()
    {
        if (!File.Exists(_registrationFilePath))
        {
            throw new FileNotFoundException($"Test registration file not found: {_registrationFilePath}");
        }

        string json = File.ReadAllText(_registrationFilePath);
        var registrationList = JsonUtility.FromJson<TestRegistrationList>(json);

        return registrationList.tests ?? new List<TestRegistration>();
    }

    private List<TestRegistration> FilterTests(List<TestRegistration> registrations)
    {
        return registrations.Where(r =>
        {
            if (!r.enabled)
                return false;

            if (_tagsFilter == null || _tagsFilter.Count == 0)
                return true;

            return r.tags != null && r.tags.Any(tag => _tagsFilter.Contains(tag));
        }).ToList();
    }

    private async Task<TestResult> RunSingleTest(TestRegistration registration)
    {
        var startTime = DateTime.Now;

        var tester = TesterFactory.CreateTester(registration.testerId);
        tester.Initialize(registration);

        var result = await tester.RunTests();
        result.executionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;

        return result;
    }

    /// <summary>
    /// Сохранение результата теста в соответствующую папку (passed/failed)
    /// </summary>
    private void SaveTestResult(string testId, TestResult result)
    {
        // Определяем директорию в зависимости от результата
        string targetDir = result.success ? _passedTestsDir : _failedTestsDir;

        string fileName = $"{testId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(targetDir, fileName);

        string json = JsonUtility.ToJson(result, true);
        File.WriteAllText(filePath, json);

        string status = result.success ? "PASSED" : "FAILED";
        Debug.Log($"Test result ({status}) saved to: {filePath}");
    }

    /// <summary>
    /// Сохранение summary отчета (только failed тесты, без testCases)
    /// </summary>
    private void SaveSummaryReport(List<TestResult> results)
    {
        // Создаем копии failed результатов БЕЗ testCases
        var failedResultsForSummary = results
            .Where(r => !r.success)
            .Select(r => new TestResult
            {
                testId = r.testId,
                testerId = r.testerId,
                success = r.success,
                message = r.message,
                executionTimeMs = r.executionTimeMs,
                additionalData = r.additionalData,
                testCases = new List<TestCaseResult>() // Пустой список вместо всех test cases
            })
            .ToList();

        var summary = new TestSummary
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            totalTests = results.Count,
            passedTests = results.Count(r => r.success),
            failedTests = results.Count(r => !r.success),
            totalExecutionTimeMs = results.Sum(r => r.executionTimeMs),
            results = failedResultsForSummary // Только failed тесты, без testCases
        };

        string fileName = $"summary_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(_summaryDir, fileName);

        string json = JsonUtility.ToJson(summary, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"Summary report saved to: {filePath}");
        Debug.Log($"Summary: {summary.passedTests}/{summary.totalTests} tests passed");

        if (summary.failedTests > 0)
        {
            Debug.LogWarning($"Failed tests included in summary: {summary.failedTests}");
        }
    }
}

/// <summary>
/// Сериализуемый класс для summary отчета
/// </summary>
[Serializable]
public class TestSummary
{
    public string timestamp;
    public int totalTests;
    public int passedTests;
    public int failedTests;
    public long totalExecutionTimeMs;

    // Только failed тесты (success == false), без testCases
    public List<TestResult> results;
}