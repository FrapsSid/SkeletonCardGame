using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TestRegistration
{
    public string testId;
    public string testerId;
    public string configPath;
    public Dictionary<string, object> parameters;
    public List<string> tags;
    public bool enabled;
}

[Serializable]
public class TestRegistrationList
{
    public List<TestRegistration> tests;
}