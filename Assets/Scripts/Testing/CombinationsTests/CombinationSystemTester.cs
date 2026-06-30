using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Combinations.Rules;

namespace Combinations.Tests
{
    public class CombinationSystemTester : ISystemTester
    {
        private TestRegistration _registration;
        private CombinationTestConfig _config;

        private static readonly JsonSerializerSettings JsonSettings =
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

        public string GetTesterId()
        {
            return "combination_system_tester";
        }

        public void Initialize(TestRegistration registration)
        {
            _registration = registration;

            if (!string.IsNullOrEmpty(registration.configPath) && File.Exists(registration.configPath))
            {
                string configJson = File.ReadAllText(registration.configPath);

                _config = JsonConvert.DeserializeObject<CombinationTestConfig>(
                    configJson,
                    JsonSettings
                );
            }
            else
            {
                throw new FileNotFoundException(
                    $"Config file not found: {registration.configPath}"
                );
            }

            Console.WriteLine(
                $"[CombinationSystemTester] Initialized with {_config?.testCases?.Count ?? 0} test cases"
            );
        }

        public async Task<TestResult> RunTests()
        {
            var result = new TestResult
            {
                testId = _registration.testId,
                testerId = GetTesterId(),
                success = true,
                message = "",
                testCases = new List<TestCaseResult>(),
                additionalData = new List<AdditionalDataItem>()
            };

            if (_config?.testCases == null || _config.testCases.Count == 0)
            {
                result.success = false;
                result.message = "No test cases found in config";
                return result;
            }

            int totalTests = 0;
            int passedTests = 0;
            int failedTests = 0;

            try
            {
                foreach (var testCase in _config.testCases)
                {
                    totalTests++;

                    var testCaseResult = await RunSingleTestCase(testCase);
                    result.testCases.Add(testCaseResult);

                    if (testCaseResult.passed)
                        passedTests++;
                    else
                    {
                        failedTests++;
                        result.success = false;
                    }

                    await Task.Yield();
                }

                result.AddAdditionalData("totalTests", totalTests);
                result.AddAdditionalData("passedTests", passedTests);
                result.AddAdditionalData("failedTests", failedTests);
                result.AddAdditionalData(
                    "passRate",
                    totalTests > 0
                        ? $"{passedTests * 100.0 / totalTests:F2}%"
                        : "0%"
                );
                result.AddAdditionalData("configPath", _registration.configPath);

                result.message = result.success
                    ? $"All {totalTests} tests passed"
                    : $"{failedTests} of {totalTests} tests failed";

                Console.WriteLine(
                    $"[CombinationSystemTester] Completed: {passedTests}/{totalTests} tests passed"
                );
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = $"Test execution error: {ex.Message}";

                var errorCase = new TestCaseResult
                {
                    name = "Exception",
                    passed = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                };

                result.testCases.Add(errorCase);

                Console.Error.WriteLine(
                    $"[CombinationSystemTester] Error: {ex.Message}\n{ex.StackTrace}"
                );
            }

            return result;
        }

        private Task<TestCaseResult> RunSingleTestCase(CombinationTestCase testCase)
        {
            var testCaseResult = new TestCaseResult
            {
                name = $"{testCase.testCaseId}: {testCase.description}",
                passed = false,
                error = "",
                stackTrace = ""
            };

            try
            {
                var combination = CreateCombination(testCase.combinationType);

                if (combination == null)
                {
                    testCaseResult.error =
                        $"Unknown combination type: {testCase.combinationType}";
                    return Task.FromResult(testCaseResult);
                }

                var cards = testCase.cards
                    .Select(tc => tc.ToCardWithPool())
                    .ToList();

                bool actualResult = combination.IsSatisfied(cards);
                bool testPassed = (actualResult == testCase.shouldPass);

                testCaseResult.passed = testPassed;

                if (!testPassed)
                {
                    string expected = testCase.shouldPass ? "PASS" : "FAIL";
                    string actual = actualResult ? "PASS" : "FAIL";

                    testCaseResult.error =
                        $"Expected: {expected}, Actual: {actual}";

                    Console.WriteLine(
                        $"[TEST FAILED] {testCase.testCaseId}: {testCase.description}"
                    );
                    Console.WriteLine(
                        $"  Expected: {expected}, Got: {actual}"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"[TEST PASSED] {testCase.testCaseId}: {testCase.description}"
                    );
                }
            }
            catch (Exception ex)
            {
                testCaseResult.passed = false;
                testCaseResult.error = ex.Message;
                testCaseResult.stackTrace = ex.StackTrace;

                Console.Error.WriteLine(
                    $"[TEST ERROR] {testCase.testCaseId}: {ex.Message}"
                );
            }

            return Task.FromResult(testCaseResult);
        }

        private Combination CreateCombination(string combinationType)
        {
            var typeMap = new Dictionary<string, Func<Combination>>
            {
                { "ThreeInARow", () => new Combination(new ThreeInARow()) },
                { "FourInARow", () => new Combination(new FourInARow()) },
                { "FiveInARow", () => new Combination(new FiveInARow()) },
                { "SuitedLadder3", () => new Combination(new SuitedLadder3()) },
                { "SuitedLadder4", () => new Combination(new SuitedLadder4()) },
                { "StraightFlush", () => new Combination(new StraightFlush()) },
                { "ThreeOfAKind", () => new Combination(new ThreeOfAKind()) },
                { "TwoPair", () => new Combination(new TwoPair()) },
                { "DoubleStep", () => new Combination(new DoubleStep()) },
                { "TripleStep", () => new Combination(new TripleStep()) },
                { "FullHouse", () => new Combination(new FullHouse()) },
                { "DrawBridge", () => new Combination(new DrawBridge()) },
                { "BrokenSeal", () => new Combination(new BrokenSeal()) },
                { "SkewTower", () => new Combination(new SkewTower()) },
                { "DoubleFork", () => new Combination(new DoubleFork()) },
                { "RedBlackCastle", () => new Combination(new RedBlackCastle()) },
                { "TwoMasks", () => new Combination(new TwoMasks()) },
                { "Hinge", () => new Combination(new Hinge()) },
                { "Pincers", () => new Combination(new Pincers()) },
                { "TwoPairSplit", () => new Combination(new TwoPairSplit()) },
                { "SuitCross", () => new Combination(new SuitCross()) },
                { "Reflection", () => new Combination(new Reflection()) }
            };

            if (typeMap.TryGetValue(combinationType, out var factory))
                return factory();

            Console.Error.WriteLine(
                $"Unknown combination type: {combinationType}"
            );

            return null;
        }
    }
}