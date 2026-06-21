using System.Collections.Generic;
using Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuickGameLobbyScreen : LobbyScreenBase
{
    public override ScreenId Id => ScreenId.QuickGameLobby;
    protected override string Title => "БЫСТРАЯ ИГРА";

    protected override void RefreshPlayers(IReadOnlyList<NetworkPlayer> players, int maxPlayers)
    {
        GameUIFactory.ClearChildren(playerRoot);
        HorizontalLayoutGroup layout = GameUIFactory.HorizontalLayout(playerRoot.gameObject, 34f, TextAnchor.MiddleCenter);
        layout.padding = new RectOffset(12, 12, 8, 8);

        for (int i = 0; i < maxPlayers; i++)
        {
            NetworkPlayer player = i < players.Count ? players[i] : null;
            CreatePlayerBadge(playerRoot, player, "ПРИГЛАСИТЬ");
        }
    }
}
