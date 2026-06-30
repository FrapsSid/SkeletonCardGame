using System;
using System.Collections.Generic;

[Serializable]
public class TestResult
{
    public string testId;
    public string testerId;
    public bool success;
    public string message;
    public long executionTimeMs;

    // Заменяем Dictionary на List для сериализации
    public List<AdditionalDataItem> additionalData;
    public List<TestCaseResult> testCases;

    public TestResult()
    {
        additionalData = new List<AdditionalDataItem>();
        testCases = new List<TestCaseResult>();
    }

    /// <summary>
    /// Добавить дополнительные данные
    /// </summary>
    public void AddAdditionalData(string key, object value)
    {
        additionalData.Add(new AdditionalDataItem
        {
            key = key,
            value = value?.ToString() ?? "null"
        });
    }
}

[Serializable]
public class AdditionalDataItem
{
    public string key;
    public string value;
}

[Serializable]
public class TestCaseResult
{
    public string name;
    public bool passed;
    public string error;
    public string stackTrace;
}