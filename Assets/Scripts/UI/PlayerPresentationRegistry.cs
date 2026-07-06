using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("UI/Player Presentation Registry")]
[DisallowMultipleComponent]
public sealed class PlayerPresentationRegistry : MonoBehaviour
{
    private sealed class PlayerOverride
    {
        public string DisplayName = string.Empty;
        public Sprite AvatarSprite;
        public Multiplayer.NetworkPlayer NetworkPlayer;
        public Action<string> NetworkNameChangedHandler;
    }

    [Header("Game")]
    [SerializeField] private GameManager gameManager;

    [Header("Fallback Presentation")]
    [SerializeField] private Sprite defaultAvatarSprite;
    [SerializeField] private string localPlayerName = "Player";
    [SerializeField] private string aiPlayerName = "AI";
    [SerializeField] private string numberedPlayerName = "Player {0}";

    [Header("Team Colors")]
    [SerializeField] private Color redTeamColor = new Color(0.93f, 0.22f, 0.18f, 1f);
    [SerializeField] private Color blueTeamColor = new Color(0.18f, 0.48f, 0.95f, 1f);
    [SerializeField] private Color neutralTeamColor = Color.white;

    private readonly Dictionary<Skeleton, PlayerOverride> overrides = new();

    public event Action<Skeleton> PresentationChanged;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        foreach (PlayerOverride playerOverride in overrides.Values)
        {
            UnsubscribeFromNetworkPlayer(playerOverride);
        }
    }

    public static PlayerPresentationRegistry EnsureDefaultFor(GameManager manager)
    {
        PlayerPresentationRegistry registry = FindFirstObjectByType<PlayerPresentationRegistry>();
        if (registry != null)
        {
            registry.AssignGameManager(manager);
            return registry;
        }

        GameObject registryObject = new GameObject("Player Presentation Registry");
        registry = registryObject.AddComponent<PlayerPresentationRegistry>();
        registry.AssignGameManager(manager);
        return registry;
    }

    public void AssignGameManager(GameManager manager)
    {
        gameManager = manager;
        ResolveReferences();
    }

    public PlayerPresentation Resolve(Skeleton player)
    {
        ResolveReferences();

        int playerIndex = gameManager != null && player != null ? IndexOf(gameManager.Players, player) : -1;
        bool isLocalPlayer = gameManager != null && player != null && gameManager.LocalPlayer == player;
        string displayName = ResolveDisplayName(player, playerIndex, isLocalPlayer);

        return new PlayerPresentation(
            player,
            displayName,
            ResolveAvatar(player),
            ResolveTeamColor(player),
            playerIndex,
            isLocalPlayer,
            player != null && player.HasNetworkClientId,
            player != null ? player.NetworkClientId : 0);
    }

    public void RegisterNetworkPlayer(Skeleton player, Multiplayer.NetworkPlayer networkPlayer)
    {
        if (player == null || networkPlayer == null)
            return;

        player.SetNetworkClientId(networkPlayer.ClientId);

        PlayerOverride playerOverride = GetOrCreateOverride(player);
        if (playerOverride.NetworkPlayer != networkPlayer)
        {
            UnsubscribeFromNetworkPlayer(playerOverride);
            playerOverride.NetworkPlayer = networkPlayer;
            playerOverride.NetworkNameChangedHandler = name => SetDisplayName(player, name);
            playerOverride.NetworkPlayer.OnPlayerNameChanged += playerOverride.NetworkNameChangedHandler;
        }

        SetDisplayName(player, ResolveNetworkName(networkPlayer));
    }

    public void SetDisplayName(Skeleton player, string displayName)
    {
        if (player == null)
            return;

        PlayerOverride playerOverride = GetOrCreateOverride(player);
        IReadOnlyList<Skeleton> players = gameManager != null ? gameManager.Players : null;
        string fallback = ResolveDisplayName(player, IndexOf(players, player), gameManager != null && gameManager.LocalPlayer == player);
        playerOverride.DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : fallback;
        PresentationChanged?.Invoke(player);
    }

    public void SetAvatar(Skeleton player, Sprite avatarSprite)
    {
        if (player == null)
            return;

        PlayerOverride playerOverride = GetOrCreateOverride(player);
        playerOverride.AvatarSprite = avatarSprite;
        PresentationChanged?.Invoke(player);
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (defaultAvatarSprite == null)
            defaultAvatarSprite = Resources.Load<Sprite>("UI/SkeletonAvatarPlaceholder");
    }

    private PlayerOverride GetOrCreateOverride(Skeleton player)
    {
        if (!overrides.TryGetValue(player, out PlayerOverride playerOverride))
        {
            playerOverride = new PlayerOverride();
            overrides.Add(player, playerOverride);
        }

        return playerOverride;
    }

    private string ResolveDisplayName(Skeleton player, int playerIndex, bool isLocalPlayer)
    {
        if (player != null && overrides.TryGetValue(player, out PlayerOverride playerOverride) && !string.IsNullOrWhiteSpace(playerOverride.DisplayName))
            return playerOverride.DisplayName;

        if (player != null && player.HasNetworkClientId)
            return string.Format(numberedPlayerName, player.NetworkClientId);

        if (isLocalPlayer)
            return localPlayerName;

        if (gameManager != null && gameManager.Players.Count <= 2)
            return aiPlayerName;

        return string.Format(numberedPlayerName, playerIndex >= 0 ? playerIndex + 1 : 1);
    }

    private Sprite ResolveAvatar(Skeleton player)
    {
        if (player != null && overrides.TryGetValue(player, out PlayerOverride playerOverride) && playerOverride.AvatarSprite != null)
            return playerOverride.AvatarSprite;

        return defaultAvatarSprite;
    }

    private Color ResolveTeamColor(Skeleton player)
    {
        if (player == null || gameManager == null)
            return neutralTeamColor;

        int teamIndex = IndexOf(gameManager.Teams, player.team);
        if (teamIndex < 0)
            return neutralTeamColor;

        return teamIndex % 2 == 0 ? redTeamColor : blueTeamColor;
    }

    private static string ResolveNetworkName(Multiplayer.NetworkPlayer networkPlayer)
    {
        string playerName = networkPlayer.PlayerName;
        return !string.IsNullOrWhiteSpace(playerName)
            ? playerName
            : $"Player {networkPlayer.ClientId}";
    }

    private static int IndexOf<T>(IReadOnlyList<T> items, T value) where T : class
    {
        if (items == null)
            return -1;

        for (int i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], value))
                return i;
        }

        return -1;
    }

    private static void UnsubscribeFromNetworkPlayer(PlayerOverride playerOverride)
    {
        if (playerOverride.NetworkPlayer == null)
            return;

        if (playerOverride.NetworkNameChangedHandler != null)
            playerOverride.NetworkPlayer.OnPlayerNameChanged -= playerOverride.NetworkNameChangedHandler;

        playerOverride.NetworkNameChangedHandler = null;
        playerOverride.NetworkPlayer = null;
    }
}
