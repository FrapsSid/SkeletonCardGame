using System.Collections.Generic;
using TMPro;
using UnityEngine;

using Combinations;
using CardGameRound = CardGame.Round;

[AddComponentMenu("UI/Round Combination Display UI")]
[DisallowMultipleComponent]
public class RoundCombinationDisplayUI : MonoBehaviour
{
    private const string RuntimeTextName = "Runtime Combination Text";

    [Header("Game")]
    [SerializeField] private GameManager gameManager = null;

    [Header("Text Fields")]
    [SerializeField] private TMP_Text easyText = null;
    [SerializeField] private TMP_Text mediumText = null;
    [SerializeField] private TMP_Text hardText = null;
    [SerializeField] private TMP_Text antiText = null;

    [Header("Auto Text")]
    [SerializeField] private bool autoCreateTextUnderFieldRoots = true;
    [SerializeField] private bool includeDescriptions = true;
    [SerializeField] private string emptyText = "-";

    [Header("Highlighting")]
    [SerializeField] private Color satisfiedColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color defaultColor = Color.white;

    private GameManager subscribedManager = null;
    private CardGame subscribedGame = null;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToManager(gameManager);
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
    }

    public void Refresh()
    {
        RoundCombinationSet combinations = null;
        if (gameManager != null && gameManager.CardGame != null && gameManager.CardGame.round != null)
            combinations = gameManager.CardGame.round.Combinations;

        SetText(easyText, FormatCombination("Easy", combinations != null ? combinations.easyCombination : null));
        SetText(mediumText, FormatCombination("Medium", combinations != null ? combinations.mediumCombination : null));
        SetText(hardText, FormatCombination("Hard", combinations != null ? combinations.hardCombination : null));
        SetText(antiText, FormatCombination("Anti", combinations != null ? combinations.antiCombination : null));
        var personalPool = BuildPersonalPool();
        ApplyHighlight(easyText, combinations?.easyCombination, personalPool);
    }
    private List<CardWithPool> BuildPersonalPool() {
        var pool = new List<CardWithPool>();
        if (gameManager == null || gameManager.LocalPlayer == null || gameManager.CardGame?.round == null)
            return pool;

        foreach (var card in gameManager.LocalPlayer.Hand.GetCards())
            pool.Add(new CardWithPool(card, CardPool.Player1Hand));

        foreach (var card in gameManager.CardGame.round.TableCards)
            pool.Add(new CardWithPool(card, CardPool.Table));

        return pool;
    }

    private void ApplyHighlight(TMP_Text text, Combination combination, List<CardWithPool> pool) {
        if (text == null) return;
        bool satisfied = combination != null && combination.IsSatisfied(pool);
        text.color = satisfied ? satisfiedColor : defaultColor;
    }

    private string FormatCombination(string label, Combination combination)
    {
        if (combination == null)
            return $"{label}\n{emptyText}";

        string displayName = !string.IsNullOrWhiteSpace(combination.DisplayName)
            ? combination.DisplayName
            : emptyText;

        if (!includeDescriptions || string.IsNullOrWhiteSpace(combination.Description))
            return $"{label}\n{displayName}";

        return $"{label}\n{displayName}\n{combination.Description}";
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        easyText ??= ResolveFieldText("easy");
        mediumText ??= ResolveFieldText("medium");
        hardText ??= ResolveFieldText("hard");
        antiText ??= ResolveFieldText("anti");
    }

    private TMP_Text ResolveFieldText(string fieldName)
    {
        Transform field = FindChildRecursive(transform, fieldName);
        if (field == null)
            return null;

        TMP_Text existing = field.GetComponentInChildren<TMP_Text>(true);
        if (existing != null)
            return existing;

        if (!autoCreateTextUnderFieldRoots || field is not RectTransform fieldRect)
            return null;

        GameObject textObject = new GameObject(RuntimeTextName, typeof(RectTransform));
        textObject.transform.SetParent(fieldRect, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = 14f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 7f;
        text.fontSizeMax = 16f;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.color = Color.white;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.12f;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(6f, 4f);
        rect.offsetMax = new Vector2(-6f, -4f);
        rect.SetAsLastSibling();

        return text;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (string.Equals(root.name, childName, System.StringComparison.OrdinalIgnoreCase))
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
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
        subscribedGame.OnCardTaken += HandleCardsChanged;
        subscribedGame.OnTableCardsDealt += HandleTableCardsDealt;
    }

    private void UnsubscribeFromGame()
    {
        if (subscribedGame == null)
            return;

        subscribedGame.OnPhaseChanged -= HandlePhaseChanged;
        subscribedGame.OnRoundStarted -= HandleRoundChanged;
        subscribedGame.OnBettingRoundStarted -= HandleRoundChanged;
        subscribedGame.OnBettingRoundEnded -= HandleRoundChanged;
        subscribedGame.OnCardTaken -= HandleCardsChanged;
        subscribedGame.OnTableCardsDealt -= HandleTableCardsDealt;
        subscribedGame = null;
    }

    private void HandleCardsChanged(Skeleton player, CardData card) => Refresh();
    private void HandleTableCardsDealt(IReadOnlyList<CardData> cards) => Refresh();

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
        Refresh();
    }
#endif
}
