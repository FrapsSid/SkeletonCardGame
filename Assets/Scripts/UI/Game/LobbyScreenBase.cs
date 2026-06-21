using System;
using System.Collections.Generic;
using Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class LobbyScreenBase : GameUIScreen
{
    protected bool isHost;
    protected RectTransform playerRoot;
    protected TMP_Text roomCodeText;
    protected TMP_Text statusText;
    protected Button startButton;

    protected abstract string Title { get; }

    public void SetRole(bool host)
    {
        isHost = host;
        Refresh();
    }

    protected override void Build()
    {
        GameUIFactory.Backdrop(transform, darkness: 0.64f);

        RectTransform panel = GameUIFactory.Panel(transform, "LobbyPanel", GameUITheme.Panel);
        GameUIFactory.Anchor(panel, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        TMP_Text title = GameUIFactory.Text(panel, "Title", Title, 64f, TextAlignmentOptions.Center, GameUITheme.White);
        title.characterSpacing = 1.5f;
        GameUIFactory.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(0f, 86f));

        roomCodeText = GameUIFactory.Text(panel, "RoomCode", string.Empty, 20f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.34f), FontStyles.Bold);
        GameUIFactory.Anchor(roomCodeText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(0f, 34f));

        playerRoot = GameUIFactory.CreateRect("Players", panel);
        CreatePlayerArea(playerRoot);

        statusText = GameUIFactory.Text(panel, "Status", string.Empty, 26f, TextAlignmentOptions.Center, GameUITheme.MutedWhite, FontStyles.Bold);
        GameUIFactory.Anchor(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(0f, 40f));

        startButton = GameUIFactory.Button(panel, "StartButton", "СТАРТ", StartGame, true);
        GameUIFactory.Anchor(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 136f), new Vector2(260f, 64f));

        Button back = GameUIFactory.Button(panel, "BackButton", "ВЫХОД", LeaveLobby, true);
        GameUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(260f, 58f));
    }

    protected override void OnShow()
    {
        SubscribeNetwork();
        Refresh();
    }

    protected override void OnHide()
    {
        UnsubscribeNetwork();
    }

    protected virtual void CreatePlayerArea(RectTransform root)
    {
        GameUIFactory.Anchor(root, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    protected abstract void RefreshPlayers(IReadOnlyList<NetworkPlayer> players, int maxPlayers);

    protected void Refresh()
    {
        if (playerRoot == null)
            return;

        IReadOnlyList<NetworkPlayer> players = GetPlayers();
        int maxPlayers = NetworkGameManager.Instance != null ? NetworkGameManager.Instance.MaxPlayers : 4;

        roomCodeText.text = NetworkGameManager.Instance != null
            ? "КОД ЛОББИ: 127.0.0.1"
            : "КОД ЛОББИ: НЕТ СЕТЕВОГО МЕНЕДЖЕРА";
        startButton.gameObject.SetActive(isHost);
        statusText.text = players.Count == 0 ? "ОЖИДАНИЕ ИГРОКОВ" : $"ИГРОКОВ: {players.Count}/{maxPlayers}";

        RefreshPlayers(players, maxPlayers);
    }

    protected void CreatePlayerBadge(Transform parent, NetworkPlayer player, string emptyLabel)
    {
        RectTransform slot = GameUIFactory.CreateRect("PlayerSlot", parent);
        if (parent.GetComponent<HorizontalOrVerticalLayoutGroup>() != null)
            GameUIFactory.Layout(slot.gameObject, 150f, 150f);
        else
            GameUIFactory.Stretch(slot);

        VerticalLayoutGroup layout = GameUIFactory.VerticalLayout(slot.gameObject, 6f, TextAnchor.MiddleCenter);

        Button avatar = GameUIFactory.Button(slot, "Avatar", player != null ? Initials(player.PlayerName) : emptyLabel, player == null ? InvitePlayer : null, false);
        GameUIFactory.Layout(avatar.gameObject, 96f, 96f);

        TMP_Text avatarText = avatar.GetComponentInChildren<TMP_Text>();
        if (avatarText != null)
            avatarText.fontSize = player != null ? 30f : 14f;

        Image avatarImage = avatar.GetComponent<Image>();
        if (avatarImage != null)
        {
            GameUIFactory.UseCircleSprite(avatarImage);
            avatarImage.color = player != null ? new Color(0.02f, 0.02f, 0.02f, 0.92f) : new Color(0.42f, 0.46f, 0.44f, 0.82f);
        }

        TMP_Text name = GameUIFactory.Text(slot, "Name", player != null ? SafeName(player) : string.Empty, 18f, TextAlignmentOptions.Center, GameUITheme.White, FontStyles.Bold);
        GameUIFactory.Layout(name.gameObject, 150f, 28f);
    }

    protected static string SafeName(NetworkPlayer player)
    {
        if (player == null || string.IsNullOrWhiteSpace(player.PlayerName))
            return "Player";
        return player.PlayerName;
    }

    private IReadOnlyList<NetworkPlayer> GetPlayers()
    {
        return NetworkGameManager.Instance != null
            ? NetworkGameManager.Instance.ConnectedPlayers
            : Array.Empty<NetworkPlayer>();
    }

    private void SubscribeNetwork()
    {
        if (NetworkGameManager.Instance == null)
            return;

        NetworkGameManager.Instance.OnPlayerConnected += HandlePlayerConnected;
        NetworkGameManager.Instance.OnPlayerDisconnected += HandlePlayerDisconnected;
        NetworkGameManager.Instance.OnDisconnected += HandleDisconnected;
    }

    private void UnsubscribeNetwork()
    {
        if (NetworkGameManager.Instance == null)
            return;

        NetworkGameManager.Instance.OnPlayerConnected -= HandlePlayerConnected;
        NetworkGameManager.Instance.OnPlayerDisconnected -= HandlePlayerDisconnected;
        NetworkGameManager.Instance.OnDisconnected -= HandleDisconnected;
    }

    private void HandlePlayerConnected(NetworkPlayer player) => Refresh();
    private void HandlePlayerDisconnected(NetworkPlayer player) => Refresh();

    private void HandleDisconnected(DisconnectReason reason)
    {
        statusText.text = $"СЕССИЯ ЗАВЕРШЕНА: {reason}";
        Refresh();
    }

    private void InvitePlayer()
    {
        statusText.text = "ПРИГЛАШЕНИЕ НЕ ПОДКЛЮЧЕНО";
    }

    private void LeaveLobby()
    {
        UI.ReturnToMainMenu();
    }

    private void StartGame()
    {
        UI.RefreshGameManager();
        if (UI.GameManager != null)
            UI.GameManager.StartGame();

        UI.EnterGameHud();
    }

    private static string Initials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "P";

        string trimmed = name.Trim();
        return trimmed.Length <= 2 ? trimmed.ToUpperInvariant() : trimmed.Substring(0, 2).ToUpperInvariant();
    }
}
