using System.Threading.Tasks;

public interface ISystemTester
{
    /// <summary>
    /// Инициализация тестера с конфигурацией
    /// </summary>
    void Initialize(TestRegistration registration);

    /// <summary>
    /// Запуск тестов и возврат результата
    /// </summary>
    Task<TestResult> RunTests();

    /// <summary>
    /// ID тестера
    /// </summary>
    string GetTesterId();
}