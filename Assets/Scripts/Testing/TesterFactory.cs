using System;
using System.Collections.Generic;

public class TesterFactory
{
    private static Dictionary<string, Type> _registeredTesters = new Dictionary<string, Type>();

    /// <summary>
    /// Регистрация тестера в фабрике
    /// </summary>
    public static void RegisterTester(string testerId, Type testerType)
    {
        if (!typeof(ISystemTester).IsAssignableFrom(testerType))
        {
            throw new ArgumentException($"Type {testerType.Name} does not implement ISystemTester");
        }

        _registeredTesters[testerId] = testerType;
    }

    /// <summary>
    /// Создание экземпляра тестера по ID
    /// </summary>
    public static ISystemTester CreateTester(string testerId)
    {
        if (!_registeredTesters.ContainsKey(testerId))
        {
            throw new ArgumentException($"Tester with ID '{testerId}' is not registered");
        }

        return (ISystemTester)Activator.CreateInstance(_registeredTesters[testerId]);
    }

    /// <summary>
    /// Автоматическая регистрация всех тестеров в сборке
    /// </summary>
    public static void AutoRegisterTesters()
    {
        var testerTypes = ReflectionHelper.FindAllImplementations<ISystemTester>();

        foreach (var type in testerTypes)
        {
            var instance = (ISystemTester)Activator.CreateInstance(type);
            RegisterTester(instance.GetTesterId(), type);
        }
    }
}

// Вспомогательный класс для рефлексии
public static class ReflectionHelper
{
    public static List<Type> FindAllImplementations<T>()
    {
        var interfaceType = typeof(T);
        var types = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (interfaceType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        types.Add(type);
                    }
                }
            }
            catch
            {
                // Игнорируем сборки, которые не можем загрузить
            }
        }

        return types;
    }
}