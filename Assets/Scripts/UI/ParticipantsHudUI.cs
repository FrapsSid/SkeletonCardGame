using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using CardGameRound = CardGame.Round;

[AddComponentMenu("UI/Participants HUD UI")]
[DisallowMultipleComponent]
public sealed class ParticipantsHudUI : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerPresentationRegistry presentationRegistry;
    [SerializeField] private Multiplayer.NetworkGameState networkGameState;

    [Header("Entries")]
    [SerializeField] private RectTransform entriesRoot;
    [SerializeField] private ParticipantHudEntryUI entryPrefab;
    [SerializeField] private int maxVisiblePlayers = 4;

    [Header("Fallback UI")]
    [SerializeField] private bool buildRuntimeLayoutWhenMissing = true;
    [SerializeField] private Vector2 runtimeSize = new Vector2(350f, 325f);
    [SerializeField] private Vector2 runtimeOffset = new Vector2(-24f, -24f);

    private readonly Dictionary<Skeleton, ParticipantHudEntryUI> entries = new();
    private readonly List<Skeleton> visiblePlayers = new();
    private GameManager subscribedManager;
    private CardGame subscribedGame;
    private Multiplayer.NetworkGameState subscribedNetworkState;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToManager(gameManager);
        SubscribeToNetworkState(networkGameState);
        SubscribeToRegistry(presentationRegistry);
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
        UnsubscribeFromNetworkState();
        UnsubscribeFromRegistry();
    }

    public static ParticipantsHudUI EnsureDefaultFor(GameManager manager)
    {
        ParticipantsHudUI hud = FindFirstObjectByType<ParticipantsHudUI>();
        if (hud != null)
        {
            hud.AssignGameManager(manager);
            return hud;
        }

        GameObject canvasObject = new GameObject("Participants HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject hudObject = new GameObject("Participants HUD", typeof(RectTransform), typeof(ParticipantsHudUI));
        hudObject.transform.SetParent(canvasObject.transform, false);

        hud = hudObject.GetComponent<ParticipantsHudUI>();
        hud.AssignGameManager(manager);
        hud.ConfigureRuntimeRect();
        hud.ResolveReferences();
        hud.Refresh();
        return hud;
    }

    public void AssignGameManager(GameManager manager)
    {
        gameManager = manager;
        ResolveReferences();
        SubscribeToManager(gameManager);
        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();
        visiblePlayers.Clear();

        if (gameManager == null || entriesRoot == null)
        {
            ClearEntries();
            return;
        }

        int count = Mathf.Min(maxVisiblePlayers, gameManager.Players.Count);
        for (int i = 0; i < count; i++)
        {
            Skeleton player = gameManager.Players[i];
            if (player != null)
                visiblePlayers.Add(player);
        }

        RemoveMissingEntries();

        int activePlayerIndex = ResolveActivePlayerIndex();
        for (int i = 0; i < visiblePlayers.Count; i++)
        {
            Skeleton player = visiblePlayers[i];
            ParticipantHudEntryUI entry = GetOrCreateEntry(player);
            entry.transform.SetSiblingIndex(i);
            entry.Bind(presentationRegistry != null ? presentationRegistry.Resolve(player) : null, i == activePlayerIndex);
        }
    }

    private void ConfigureRuntimeRect()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = runtimeOffset;
        rect.sizeDelta = runtimeSize;
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (presentationRegistry == null)
            presentationRegistry = gameManager != null
                ? PlayerPresentationRegistry.EnsureDefaultFor(gameManager)
                : FindFirstObjectByType<PlayerPresentationRegistry>();

        if (networkGameState == null)
            networkGameState = FindFirstObjectByType<Multiplayer.NetworkGameState>();

        if (entriesRoot == null && buildRuntimeLayoutWhenMissing)
            BuildRuntimeLayout();
    }

    private void BuildRuntimeLayout()
    {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect == null)
            return;

        ConfigureRuntimeRect();

        Image panelImage = GetComponent<Image>();
        if (panelImage != null)
            panelImage.enabled = false;

        GameObject entriesObject = new GameObject("Entries", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        entriesObject.transform.SetParent(transform, false);
        entriesRoot = entriesObject.GetComponent<RectTransform>();
        entriesRoot.anchorMin = Vector2.zero;
        entriesRoot.anchorMax = Vector2.one;
        entriesRoot.offsetMin = Vector2.zero;
        entriesRoot.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = entriesObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 11f;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = entriesObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private ParticipantHudEntryUI GetOrCreateEntry(Skeleton player)
    {
        if (entries.TryGetValue(player, out ParticipantHudEntryUI entry) && entry != null)
            return entry;

        entry = entryPrefab != null
            ? Instantiate(entryPrefab, entriesRoot)
            : ParticipantHudEntryUI.CreateDefault(entriesRoot);

        entries[player] = entry;
        return entry;
    }

    private void RemoveMissingEntries()
    {
        List<Skeleton> removedPlayers = null;
        foreach (KeyValuePair<Skeleton, ParticipantHudEntryUI> pair in entries)
        {
            if (!visiblePlayers.Contains(pair.Key))
            {
                removedPlayers ??= new List<Skeleton>();
                removedPlayers.Add(pair.Key);
            }
        }

        if (removedPlayers == null)
            return;

        foreach (Skeleton player in removedPlayers)
        {
            if (entries.TryGetValue(player, out ParticipantHudEntryUI entry) && entry != null)
                Destroy(entry.gameObject);

            entries.Remove(player);
        }
    }

    private void ClearEntries()
    {
        foreach (ParticipantHudEntryUI entry in entries.Values)
        {
            if (entry != null)
                Destroy(entry.gameObject);
        }

        entries.Clear();
    }

    private int ResolveActivePlayerIndex()
    {
        if (networkGameState != null
            && networkGameState.IsSpawned
            && networkGameState.CurrentTurnPlayerIndex >= 0)
        {
            return networkGameState.CurrentTurnPlayerIndex;
        }

        CardGame game = gameManager != null ? gameManager.CardGame : null;
        if (game?.round == null || game.phase != CardGame.GamePhase.Betting)
            return -1;

        return IndexOf(visiblePlayers, game.round.CurrentPlayer);
    }

    private void SubscribeToManager(GameManager manager)
    {
        if (subscribedManager == manager)
            return;

        UnsubscribeFromManager();
        subscribedManager = manager;
        if (subscribedManager == null)
            return;

        subscribedManager.OnGameCreated += HandleGameCreated;
        SubscribeToGame(subscribedManager.CardGame);
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnGameCreated -= HandleGameCreated;
        subscribedManager = null;
        UnsubscribeFromGame();
    }

    private void SubscribeToGame(CardGame game)
    {
        if (subscribedGame == game)
            return;

        UnsubscribeFromGame();
        subscribedGame = game;
        if (subscribedGame == null)
            return;

        subscribedGame.OnPhaseChanged += HandlePhaseChanged;
        subscribedGame.OnRoundStarted += HandleRoundChanged;
        subscribedGame.OnBettingRoundStarted += HandleRoundChanged;
        subscribedGame.OnBettingRoundEnded += HandleRoundChanged;
        subscribedGame.OnRoundEnded += HandleRoundEnded;
        subscribedGame.OnPlayerFolded += HandlePlayerChanged;
        subscribedGame.OnTurnStarted += HandlePlayerChanged;
        subscribedGame.OnTurnEnded += HandlePlayerChanged;
    }

    private void UnsubscribeFromGame()
    {
        if (subscribedGame == null)
            return;

        subscribedGame.OnPhaseChanged -= HandlePhaseChanged;
        subscribedGame.OnRoundStarted -= HandleRoundChanged;
        subscribedGame.OnBettingRoundStarted -= HandleRoundChanged;
        subscribedGame.OnBettingRoundEnded -= HandleRoundChanged;
        subscribedGame.OnRoundEnded -= HandleRoundEnded;
        subscribedGame.OnPlayerFolded -= HandlePlayerChanged;
        subscribedGame.OnTurnStarted -= HandlePlayerChanged;
        subscribedGame.OnTurnEnded -= HandlePlayerChanged;
        subscribedGame = null;
    }

    private void SubscribeToNetworkState(Multiplayer.NetworkGameState gameState)
    {
        if (subscribedNetworkState == gameState)
            return;

        UnsubscribeFromNetworkState();
        subscribedNetworkState = gameState;
        if (subscribedNetworkState == null)
            return;

        subscribedNetworkState.OnCurrentTurnChanged += HandleCurrentTurnChanged;
    }

    private void UnsubscribeFromNetworkState()
    {
        if (subscribedNetworkState == null)
            return;

        subscribedNetworkState.OnCurrentTurnChanged -= HandleCurrentTurnChanged;
        subscribedNetworkState = null;
    }

    private void SubscribeToRegistry(PlayerPresentationRegistry registry)
    {
        if (registry == null)
            return;

        registry.PresentationChanged -= HandlePresentationChanged;
        registry.PresentationChanged += HandlePresentationChanged;
    }

    private void UnsubscribeFromRegistry()
    {
        if (presentationRegistry == null)
            return;

        presentationRegistry.PresentationChanged -= HandlePresentationChanged;
    }

    private void HandleGameCreated(CardGame game)
    {
        SubscribeToGame(game);
        Refresh();
    }

    private void HandlePhaseChanged(CardGame.GamePhase phase)
    {
        Refresh();
    }

    private void HandleRoundChanged(CardGameRound round)
    {
        Refresh();
    }

    private void HandleRoundEnded(RoundResult result)
    {
        Refresh();
    }

    private void HandlePlayerChanged(Skeleton player)
    {
        Refresh();
    }

    private void HandleCurrentTurnChanged(int playerIndex, ulong clientId)
    {
        Refresh();
    }

    private void HandlePresentationChanged(Skeleton player)
    {
        Refresh();
    }

    private static int IndexOf(IReadOnlyList<Skeleton> players, Skeleton player)
    {
        if (players == null || player == null)
            return -1;

        for (int i = 0; i < players.Count; i++)
        {
            if (ReferenceEquals(players[i], player))
                return i;
        }

        return -1;
    }
}
