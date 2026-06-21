using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenuScreen : GameUIScreen
{
    public override ScreenId Id => ScreenId.PauseMenu;
    public override bool IsModal => true;

    protected override void Build()
    {
        RectTransform overlay = GameUIFactory.CreateRect("Overlay", transform);
        GameUIFactory.Stretch(overlay);
        GameUIFactory.Image(overlay, new Color(0f, 0f, 0f, 0.34f));

        RectTransform panel = GameUIFactory.Panel(transform, "PausePanel", new Color(0.02f, 0.025f, 0.024f, 0.5f), false);
        GameUIFactory.Anchor(panel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform menu = GameUIFactory.CreateRect("Menu", panel);
        GameUIFactory.Anchor(menu, new Vector2(0.34f, 0.38f), new Vector2(0.66f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup layout = GameUIFactory.VerticalLayout(menu.gameObject, 12f, TextAnchor.MiddleCenter);

        TMP_Text title = GameUIFactory.Text(menu, "Title", "ПАУЗА", 48f, TextAlignmentOptions.Center, GameUITheme.White);
        GameUIFactory.Layout(title.gameObject, 460f, 62f);

        Button settings = GameUIFactory.Button(menu, "SettingsButton", "НАСТРОЙКИ", () => UI.OpenSettings(true), true);
        GameUIFactory.Layout(settings.gameObject, 460f, 52f);

        Button exit = GameUIFactory.Button(menu, "ExitButton", "ВЫЙТИ ИЗ ИГРЫ", () => UI.ReturnToMainMenu(), true);
        GameUIFactory.Layout(exit.gameObject, 460f, 52f);
    }
}
