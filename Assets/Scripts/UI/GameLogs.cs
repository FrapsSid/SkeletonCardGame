using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

public class InGameConsole : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI logTextDisplay;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Console Settings")]
    [SerializeField] private int maxCharacters = 15000;
    [SerializeField] private bool autoScroll = true;

    private StringBuilder stringBuilder = new StringBuilder();

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string color = "white";

        switch (type)
        {
            case LogType.Warning:
                color = "#FFD700";
                break;
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                color = "#FF4500";
                break;
        }

        stringBuilder.AppendLine($"<color={color}>[{type}] {logString}</color>");

        if (type == LogType.Exception || type == LogType.Error)
        {
            stringBuilder.AppendLine($"<size=80%><color=#888888>{stackTrace}</color></size>");
        }

        if (stringBuilder.Length > maxCharacters)
        {
            stringBuilder.Remove(0, stringBuilder.Length - maxCharacters);
        }

        logTextDisplay.text = stringBuilder.ToString();

        if (autoScroll)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
