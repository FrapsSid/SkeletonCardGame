using UnityEngine;

public static class GameUITheme
{
    public static readonly Color Cyan = new Color(0f, 1f, 0.68f, 1f);
    public static readonly Color CyanSoft = new Color(0f, 0.85f, 0.64f, 0.65f);
    public static readonly Color CyanGlow = new Color(0f, 1f, 0.68f, 0.32f);
    public static readonly Color CyanDeepGlow = new Color(0f, 1f, 0.68f, 0.12f);
    public static readonly Color White = new Color(1f, 1f, 1f, 1f);
    public static readonly Color MutedWhite = new Color(1f, 1f, 1f, 0.68f);
    public static readonly Color Red = new Color(0.86f, 0.18f, 0.2f, 1f);
    public static readonly Color Blue = new Color(0.16f, 0.62f, 0.95f, 1f);
    public static readonly Color Dark = new Color(0.02f, 0.025f, 0.025f, 0.92f);
    public static readonly Color DarkSoft = new Color(0.02f, 0.025f, 0.025f, 0.72f);
    public static readonly Color ButtonBase = new Color(0.12f, 0.12f, 0.12f, 0.86f);
    public static readonly Color ButtonHover = new Color(0.09f, 0.26f, 0.21f, 0.94f);
    public static readonly Color ButtonPressed = new Color(0f, 0.72f, 0.48f, 0.96f);
    public static readonly Color ButtonDisabled = new Color(0.38f, 0.42f, 0.4f, 0.46f);
    public static readonly Color Panel = new Color(0.03f, 0.032f, 0.032f, 0.84f);
    public static readonly Color PanelClear = new Color(0.03f, 0.032f, 0.032f, 0.56f);

    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;

    public static float ScaleForScreen()
    {
        float widthScale = Screen.width > 0 ? Screen.width / ReferenceWidth : 1f;
        float heightScale = Screen.height > 0 ? Screen.height / ReferenceHeight : 1f;
        return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.68f, 1.28f);
    }
}
