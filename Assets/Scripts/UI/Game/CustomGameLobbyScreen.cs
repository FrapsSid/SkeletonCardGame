using System.Collections.Generic;
using Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CustomGameLobbyScreen : LobbyScreenBase
{
    public override ScreenId Id => ScreenId.CustomGameLobby;
    protected override string Title => "СВОЯ ИГРА";

    private RectTransform table;

    protected override void CreatePlayerArea(RectTransform root)
    {
        GameUIFactory.Anchor(root, new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.74f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        table = GameUIFactory.CreateRect("Table", root);
        GameUIFactory.Anchor(table, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 430f));
        Image tableImage = GameUIFactory.Image(table, new Color(0.44f, 0.36f, 0.22f, 0.48f));
        GameUIFactory.UseCircleSprite(tableImage);
        tableImage.raycastTarget = false;

        TMP_Text invite = GameUIFactory.Text(table, "Invite", "ПРИГЛАСИТЬ", 24f, TextAlignmentOptions.Center, GameUITheme.White, FontStyles.Bold);
        GameUIFactory.Anchor(invite.rectTransform, new Vector2(0f, 0.56f), new Vector2(1f, 0.76f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        TMP_Text code = GameUIFactory.Text(table, "Code", "КОД КОМНАТЫ", 18f, TextAlignmentOptions.Center, new Color(0f, 0f, 0f, 0.36f), FontStyles.Bold);
        GameUIFactory.Anchor(code.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    protected override void RefreshPlayers(IReadOnlyList<NetworkPlayer> players, int maxPlayers)
    {
        for (int i = playerRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = playerRoot.GetChild(i);
            if (table == null || child != table)
                Destroy(child.gameObject);
        }

        if (table == null)
            CreatePlayerArea(playerRoot);

        int slots = Mathf.Max(4, maxPlayers);
        for (int i = 0; i < slots; i++)
        {
            NetworkPlayer player = i < players.Count ? players[i] : null;
            RectTransform slot = GameUIFactory.CreateRect("TableSlot", playerRoot);
            float angle = 45f - (360f / slots) * i;
            Vector2 position = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * 385f, Mathf.Sin(angle * Mathf.Deg2Rad) * 250f);
            GameUIFactory.Anchor(slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(150f, 150f));

            CreatePlayerBadge(slot, player, "ПУСТО");
        }
    }
}
