using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsScreen : GameUIScreen
{
    public override ScreenId Id => ScreenId.Settings;

    protected override void Build()
    {
        GameUIFactory.Backdrop(transform, darkness: 0.62f);

        RectTransform panel = GameUIFactory.Panel(transform, "SettingsPanel", GameUITheme.Panel);
        GameUIFactory.Anchor(panel, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        TMP_Text title = GameUIFactory.Text(panel, "Title", "НАСТРОЙКИ", 62f, TextAlignmentOptions.Center, GameUITheme.White);
        GameUIFactory.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(0f, 92f));

        RectTransform emptyArea = GameUIFactory.CreateRect("FutureOptionsArea", panel);
        GameUIFactory.Anchor(emptyArea, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GameUIFactory.Image(emptyArea, new Color(0f, 0f, 0f, 0.08f)).raycastTarget = false;

        Button back = GameUIFactory.Button(panel, "BackButton", "НАЗАД", () => UI.CloseSettings(), true);
        GameUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(260f, 72f));
    }
}
