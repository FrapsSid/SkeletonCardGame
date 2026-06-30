using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class MasterTester
{
    private readonly string _registrationFilePath;
    private readonly string _outputDirectory;
    private readonly List<string> _tagsFilter;

    private readonly string _passedDir;
    private readonly string _failedDir;
    private readonly string _summaryDir;

    private static readonly JsonSerializerSettings JsonSettings =
        new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

    public MasterTester(string registrationFilePath, string outputDirectory, List<string> tagsFilter = null)
    {
        _registrationFilePath = registrationFilePath;
        _outputDirectory = outputDirectory;
        _tagsFilter = tagsFilter ?? new List<string>();

        _passedDir = Path.Combine(_outputDirectory, "passed");
        _failedDir = Path.Combine(_outputDirectory, "failed");
        _summaryDir = Path.Combine(_outputDirectory, "summary");
    }

    // ✅ Универсальный логгер
    private void Log(string message)
    {
        Console.WriteLine(message);
    }

    private void LogError(string message)
    {
        Console.Error.WriteLine(message);
    }

    public async Task<bool> RunAllTests()
    {
        try
        {
            Log("======================================");
            Log("Starting MasterTester...");
            Log("======================================");

            CreateDirectories();

            TesterFactory.AutoRegisterTesters();

            var registrations = LoadTestRegistrations();
            var testsToRun = FilterTests(registrations);

            Log($"Tests to run: {testsToRun.Count}");

            bool allPassed = true;
            var allResults = new List<TestResult>();

            foreach (var registration in testsToRun)
            {
                Log($"Running: {registration.testId}");

                var start = DateTime.UtcNow;

                var tester = TesterFactory.CreateTester(registration.testerId);
                tester.Initialize(registration);

                var result = await tester.RunTests();

                result.executionTimeMs =
                    (long)(DateTime.UtcNow - start).TotalMilliseconds;

                allResults.Add(result);

                SaveTestResult(result);

                if (!result.success)
                {
                    allPassed = false;
                    Log($"❌ FAILED: {registration.testId}");
                }
                else
                {
                    Log($"✅ PASSED: {registration.testId}");
                }
            }

            SaveSummary(allResults);

            Log("======================================");
            Log(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED");
            Log("======================================");

            return allPassed;
        }
        catch (Exception ex)
        {
            LogError("CRITICAL ERROR:");
            LogError(ex.ToString());
            return false;
        }
    }

    private void CreateDirectories()
    {
        Directory.CreateDirectory(_passedDir);
        Directory.CreateDirectory(_failedDir);
        Directory.CreateDirectory(_summaryDir);
    }

    private List<TestRegistration> LoadTestRegistrations()
    {
        if (!File.Exists(_registrationFilePath))
            throw new FileNotFoundException(_registrationFilePath);

        var json = File.ReadAllText(_registrationFilePath);

        var registrationList =
            JsonConvert.DeserializeObject<TestRegistrationList>(json);

        return registrationList?.tests ?? new List<TestRegistration>();
    }

    private List<TestRegistration> FilterTests(List<TestRegistration> registrations)
    {
        return registrations.Where(r =>
            r.enabled &&
            (_tagsFilter.Count == 0 ||
             (r.tags != null && r.tags.Any(t => _tagsFilter.Contains(t))))
        ).ToList();
    }

    private void SaveTestResult(TestResult result)
    {
        string dir = result.success ? _passedDir : _failedDir;

        string fileName = $"{result.testId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        string path = Path.Combine(dir, fileName);

        var json = JsonConvert.SerializeObject(result, JsonSettings);
        File.WriteAllText(path, json);
    }

    private void SaveSummary(List<TestResult> results)
    {
        var failedResults = results
            .Where(r => !r.success)
            .Select(r => new TestResult
            {
                testId = r.testId,
                testerId = r.testerId,
                success = r.success,
                message = r.message,
                executionTimeMs = r.executionTimeMs,
                additionalData = r.additionalData,
                testCases = new List<TestCaseResult>()
            })
            .ToList();

        var summary = new TestSummary
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            totalTests = results.Count,
            passedTests = results.Count(r => r.success),
            failedTests = results.Count(r => !r.success),
            totalExecutionTimeMs = results.Sum(r => r.executionTimeMs),
            results = failedResults
        };

        string fileName = $"summary_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        string path = Path.Combine(_summaryDir, fileName);

        var json = JsonConvert.SerializeObject(summary, JsonSettings);
        File.WriteAllText(path, json);
    }
}

public class TestSummary
{
    public string timestamp { get; set; }
    public int totalTests { get; set; }
    public int passedTests { get; set; }
    public int failedTests { get; set; }
    public long totalExecutionTimeMs { get; set; }
    public List<TestResult> results { get; set; }
}