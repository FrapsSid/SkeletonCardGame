using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuScreen : GameUIScreen
{
    public override ScreenId Id => ScreenId.MainMenu;

    protected override void Build()
    {
        RectTransform background = GameUIFactory.CreateRect("Background", transform);
        GameUIFactory.Stretch(background);
        GameUIFactory.Image(background, Color.black);

        RectTransform left = GameUIFactory.CreateRect("LeftMenu", transform);
        GameUIFactory.Anchor(left, new Vector2(0f, 0f), new Vector2(0.55f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        left.offsetMin = new Vector2(72f, 84f);
        left.offsetMax = new Vector2(-40f, -76f);

        VerticalLayoutGroup layout = GameUIFactory.VerticalLayout(left.gameObject, 28f);
        layout.padding = new RectOffset(0, 0, 54, 0);

        TMP_Text title = GameUIFactory.Text(left, "GameTitle", "SKELETON\nCARD GAME", 86f, TextAlignmentOptions.Left, GameUITheme.White);
        title.lineSpacing = -18f;
        title.characterSpacing = 2f;
        GameUIFactory.Layout(title.gameObject, 760f, 208f);

        RectTransform spacer = GameUIFactory.CreateRect("Spacer", left);
        GameUIFactory.Layout(spacer.gameObject, 1f, 30f);

        CreateMenuRow(left, "БЫСТРАЯ ИГРА", () => UI.StartQuickGameLobby());
        CreateMenuRow(left, "СВОЯ ИГРА", () => UI.StartCustomGameLobby());
        CreateMenuRow(left, "ПРИСОЕДИНИТЬСЯ", () => UI.OpenScreen(ScreenId.JoinGame));
        CreateMenuRow(left, "НАСТРОЙКИ", () => UI.OpenSettings(false));
        CreateMenuRow(left, "ВЫХОД", () => UI.ExitApplication());
    }

    private static void CreateMenuRow(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform row = GameUIFactory.CreateRect(label + " Row", parent);
        GameUIFactory.Layout(row.gameObject, 760f, 60f);
        HorizontalLayoutGroup layout = GameUIFactory.HorizontalLayout(row.gameObject, 20f, TextAnchor.MiddleLeft);

        TMP_Text dot = GameUIFactory.Text(row, "Dot", "•", 44f, TextAlignmentOptions.Center, GameUITheme.White);
        GameUIFactory.Layout(dot.gameObject, 34f, 60f);

        Button button = GameUIFactory.Button(row, label + " Button", label, onClick, true);
        GameUIFactory.Layout(button.gameObject, 650f, 60f);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.alignment = TextAlignmentOptions.Left;
            text.fontSize = 40f;
        }
    }
}
