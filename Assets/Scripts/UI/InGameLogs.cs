using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

public class InGameConsole : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI logTextDisplay; // Ссылка на ConsoleTextMesh
    [SerializeField] private ScrollRect scrollRect;          // Ссылка на сам Scroll View

    [Header("Console Settings")]
    [SerializeField] private int maxCharacters = 15000;      // Ограничение символов для оптимизации
    [SerializeField] private bool autoScroll = true;         // Должна ли консоль сама листать вниз?

    private StringBuilder stringBuilder = new StringBuilder();

    private void OnEnable()
    {
        // Подписка на системный поток логов Unity
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        // Отписка при уничтожении или скрытии окна
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string color = "white";

        // Определяем цвет строки как в редакторе Unity
        switch (type)
        {
            case LogType.Warning:
                color = "#FFD700"; // Желтый
                break;
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                color = "#FF4500"; // Красный
                break;
        }

        // Добавляем запись в буфер
        stringBuilder.AppendLine($"<color={color}>[{type}] {logString}</color>");

        // Если это ошибка, добавляем строку вызова (StackTrace) чуть меньшим шрифтом
        if (type == LogType.Exception || type == LogType.Error)
        {
            stringBuilder.AppendLine($"<size=80%><color=#888888>{stackTrace}</color></size>");
        }

        // Защита от переполнения: удаляем старые логи, если превышен лимит символов
        if (stringBuilder.Length > maxCharacters)
        {
            stringBuilder.Remove(0, stringBuilder.Length - maxCharacters);
        }

        // Выводим текст в UI
        logTextDisplay.text = stringBuilder.ToString();

        // Скроллим вниз
        if (autoScroll)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
