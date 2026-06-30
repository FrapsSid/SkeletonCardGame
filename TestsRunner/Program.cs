using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            string registrationPath = GetArgument(
                args,
                "--registration",
                "TestsRunner/Tests/test_registration.json"
            );

            string outputDir = GetArgument(
                args,
                "--output",
                "TestResults"
            );

            string tagsArg = GetArgument(args, "--tags", "");

            List<string> tags = string.IsNullOrWhiteSpace(tagsArg)
                ? new List<string>()
                : tagsArg
                    .Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

            Console.WriteLine("======================================");
            Console.WriteLine("Starting .NET Test Runner");
            Console.WriteLine($"Registration: {registrationPath}");
            Console.WriteLine($"Output: {outputDir}");
            Console.WriteLine($"Tags: {(tags.Count == 0 ? "ALL" : string.Join(", ", tags))}");
            Console.WriteLine("======================================");

            /*
             * Если у тебя ручная регистрация тестера не внутри MasterTester,
             * можно зарегистрировать здесь:
             *
             * TesterFactory.RegisterTester(
             *     "combination_system_tester",
             *     typeof(CombinationSystemTester)
             * );
             */

            var masterTester = new MasterTester(
                registrationPath,
                outputDir,
                tags
            );

            bool success = await masterTester.RunAllTests();

            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRITICAL ERROR:");
            Console.WriteLine(ex);
            return 1;
        }
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
}