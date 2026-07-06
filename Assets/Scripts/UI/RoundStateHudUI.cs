using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Round State HUD UI")]
[DisallowMultipleComponent]
public sealed class RoundStateHudUI : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BettingDiscussionGate discussionGate;

    [Header("Text")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text timerText;

    [Header("Fallback UI")]
    [SerializeField] private bool buildRuntimeLayoutWhenMissing = true;
    [SerializeField] private Vector2 runtimeSize = new Vector2(420f, 92f);
    [SerializeField] private Vector2 runtimeOffset = new Vector2(0f, -24f);

    private GameManager subscribedManager;
    private CardGame subscribedGame;
    private BettingDiscussionGate subscribedDiscussionGate;

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
        SubscribeToDiscussionGate(discussionGate);
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
        UnsubscribeFromDiscussionGate();
    }

    public static RoundStateHudUI EnsureDefaultFor(GameManager manager)
    {
        RoundStateHudUI hud = FindFirstObjectByType<RoundStateHudUI>();
        if (hud != null)
        {
            hud.AssignGameManager(manager);
            return hud;
        }

        GameObject canvasObject = new GameObject("Round State HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject hudObject = new GameObject("Round State HUD", typeof(RectTransform), typeof(RoundStateHudUI));
        hudObject.transform.SetParent(canvasObject.transform, false);

        hud = hudObject.GetComponent<RoundStateHudUI>();
        hud.AssignGameManager(manager);
        hud.ConfigureRuntimeRect();
        hud.ResolveReferences();
        hud.Refresh();
        return hud;
    }

    public void AssignGameManager(GameManager manager)
    {
        gameManager = manager;
        discussionGate = manager != null ? manager.GetComponent<BettingDiscussionGate>() : discussionGate;
        ResolveReferences();
        SubscribeToManager(gameManager);
        SubscribeToDiscussionGate(discussionGate);
        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();

        CardGame game = gameManager != null ? gameManager.CardGame : null;
        bool matchEnded = gameManager != null && gameManager.IsMatchEnded;
        CardGame.GamePhase phase = game != null ? game.phase : CardGame.GamePhase.DealingCards;

        SetPanelVisible(!matchEnded);
        if (matchEnded)
            return;

        if (phaseText != null)
            phaseText.text = ResolvePhaseLabel(phase);

        bool showTimer = discussionGate != null
            && discussionGate.IsDiscussionActive
            && phase == CardGame.GamePhase.BettingRoundStart;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(showTimer);
            timerText.text = showTimer ? FormatSeconds(discussionGate.DiscussionRemainingSeconds) : string.Empty;
        }
    }

    private void SetPanelVisible(bool visible)
    {
        Transform panel = transform.Find("Panel");
        if (panel != null && panel.gameObject.activeSelf != visible)
            panel.gameObject.SetActive(visible);
    }

    private void ConfigureRuntimeRect()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = runtimeOffset;
        rect.sizeDelta = runtimeSize;
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (discussionGate == null)
            discussionGate = gameManager != null
                ? gameManager.GetComponent<BettingDiscussionGate>()
                : FindFirstObjectByType<BettingDiscussionGate>();

        if (phaseText == null)
            phaseText = transform.Find("Panel/Phase")?.GetComponent<TMP_Text>();

        if (timerText == null)
            timerText = transform.Find("Panel/Timer")?.GetComponent<TMP_Text>();

        if (buildRuntimeLayoutWhenMissing && (phaseText == null || timerText == null))
            BuildRuntimeLayout();
    }

    private void BuildRuntimeLayout()
    {
        ConfigureRuntimeRect();

        Transform panelTransform = transform.Find("Panel");
        RectTransform panelRect;
        if (panelTransform == null)
        {
            GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(transform, false);
            panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.02f, 0.024f, 0.03f, 0.72f);

            VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 10, 10);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }
        else
        {
            panelRect = panelTransform as RectTransform;
        }

        if (phaseText == null)
            phaseText = CreateText(panelRect, "Phase", 34f, 20f, 38f, Color.white);

        if (timerText == null)
            timerText = CreateText(panelRect, "Timer", 28f, 16f, 32f, new Color(0.12f, 1f, 0.72f, 1f));
    }

    private static TMP_Text CreateText(Transform parent, string name, float preferredHeight, float minSize, float maxSize, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.color = color;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.12f;

        LayoutElement layout = textObject.GetComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.minHeight = preferredHeight;
        layout.flexibleWidth = 1f;

        return text;
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
        subscribedManager.OnMatchEnded += HandleMatchEnded;
        SubscribeToGame(subscribedManager.CardGame);
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnGameCreated -= HandleGameCreated;
        subscribedManager.OnMatchEnded -= HandleMatchEnded;
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
    }

    private void UnsubscribeFromGame()
    {
        if (subscribedGame == null)
            return;

        subscribedGame.OnPhaseChanged -= HandlePhaseChanged;
        subscribedGame = null;
    }

    private void SubscribeToDiscussionGate(BettingDiscussionGate gate)
    {
        if (subscribedDiscussionGate == gate)
            return;

        UnsubscribeFromDiscussionGate();
        subscribedDiscussionGate = gate;
        if (subscribedDiscussionGate == null)
            return;

        subscribedDiscussionGate.OnDiscussionStarted += HandleDiscussionStarted;
        subscribedDiscussionGate.OnDiscussionTimerChanged += HandleDiscussionTimerChanged;
        subscribedDiscussionGate.OnDiscussionCompleted += HandleDiscussionCompleted;
    }

    private void UnsubscribeFromDiscussionGate()
    {
        if (subscribedDiscussionGate == null)
            return;

        subscribedDiscussionGate.OnDiscussionStarted -= HandleDiscussionStarted;
        subscribedDiscussionGate.OnDiscussionTimerChanged -= HandleDiscussionTimerChanged;
        subscribedDiscussionGate.OnDiscussionCompleted -= HandleDiscussionCompleted;
        subscribedDiscussionGate = null;
    }

    private void HandleGameCreated(CardGame game)
    {
        SubscribeToGame(game);
        if (gameManager != null)
            SubscribeToDiscussionGate(gameManager.GetComponent<BettingDiscussionGate>());
        Refresh();
    }

    private void HandlePhaseChanged(CardGame.GamePhase phase)
    {
        Refresh();
    }

    private void HandleDiscussionStarted(float durationSeconds)
    {
        Refresh();
    }

    private void HandleDiscussionTimerChanged(float remainingSeconds)
    {
        Refresh();
    }

    private void HandleDiscussionCompleted(CardGame.Round round)
    {
        Refresh();
    }

    private void HandleMatchEnded(MatchEndResult result)
    {
        Refresh();
    }

    private static string ResolvePhaseLabel(CardGame.GamePhase phase)
    {
        switch (phase)
        {
            case CardGame.GamePhase.DealingCards:
                return "DEALING";
            case CardGame.GamePhase.ShowingCombinations:
                return "COMBINATIONS";
            case CardGame.GamePhase.RoundStart:
                return "ROUND START";
            case CardGame.GamePhase.BettingRoundStart:
                return "DISCUSSION";
            case CardGame.GamePhase.Betting:
                return "BETTING";
            case CardGame.GamePhase.AddingCards:
                return "ADDING CARDS";
            case CardGame.GamePhase.End:
                return "ROUND END";
            default:
                return phase.ToString().ToUpperInvariant();
        }
    }

    private static string FormatSeconds(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainder = totalSeconds % 60;
        return $"{minutes:00}:{remainder:00}";
    }
}
