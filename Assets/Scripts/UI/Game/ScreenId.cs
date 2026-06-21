using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ScreenId
{
    None,
    MainMenu,
    Settings,
    QuickGameLobby,
    CustomGameLobby,
    JoinGame,
    PauseMenu,
    TurnActionMenu,
    Inventory,
    BetScreen
}

public sealed class InventoryScreen : GameUIScreen
{
    public override ScreenId Id => ScreenId.Inventory;

    protected override void Build()
    {
        RectTransform overlay = GameUIFactory.CreateRect("Overlay", transform);
        GameUIFactory.Stretch(overlay);
        GameUIFactory.Image(overlay, new Color(0f, 0f, 0f, 0.62f));

        RectTransform frame = GameUIFactory.Panel(transform, "InventoryFrame", new Color(0.02f, 0.02f, 0.02f, 0.46f), false);
        GameUIFactory.Stretch(frame, 16f, 16f, 16f, 16f);
        frame.GetComponent<Image>().raycastTarget = false;
        GameUIFactory.AddGlow(frame.gameObject, GameUITheme.CyanSoft, GameUITheme.CyanGlow, Vector2.zero);

        TMP_Text title = GameUIFactory.Text(transform, "Title", "ИНВЕНТАРЬ", 38f, TextAlignmentOptions.Center, GameUITheme.White, FontStyles.Bold);
        GameUIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 56f));

        RectTransform gridRoot = GameUIFactory.CreateRect("Grid", transform);
        GameUIFactory.Anchor(gridRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 500f));

        GridLayoutGroup grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(142f, 142f);
        grid.spacing = new Vector2(24f, 24f);
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 9; i++)
            CreateSlot(gridRoot, i);

        Button back = GameUIFactory.Button(transform, "BackButton", "НАЗАД", () => UI.OpenScreen(ScreenId.MainMenu), true);
        GameUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0.09f), new Vector2(0.5f, 0.09f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 64f));
    }

    private static void CreateSlot(Transform parent, int index)
    {
        RectTransform slot = GameUIFactory.CreateRect("Slot " + index, parent);
        Image image = GameUIFactory.Image(slot, new Color(0.14f, 0.14f, 0.14f, 0.82f));
        GameUIFactory.UseRoundedSprite(image);
        GameUIFactory.AddGlow(slot.gameObject, GameUITheme.CyanSoft, GameUITheme.CyanGlow, new Vector2(2f, -2f));
    }
}
