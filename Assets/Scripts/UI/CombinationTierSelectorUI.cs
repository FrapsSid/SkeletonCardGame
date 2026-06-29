using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using CardGameRound = CardGame.Round;

[AddComponentMenu("UI/Combination Tier Selector UI")]
[DisallowMultipleComponent]
public class CombinationTierSelectorUI : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameManager gameManager = null;

    [Header("Buttons")]
    [SerializeField] private Button toggleButton = null;
    [SerializeField] private Button easyButton = null;
    [SerializeField] private Button mediumButton = null;
    [SerializeField] private Button hardButton = null;

    [Header("State")]
    [SerializeField] private DeclaredCombinationTier selectedTier = DeclaredCombinationTier.Easy;
    [SerializeField] private bool requireToggleBeforeSelecting = true;
    [SerializeField] private bool closeAfterSelection = true;

    [Header("Colors")]
    [SerializeField] private Color choosingTint = new Color(1f, 1f, 1f, 0.82f);
    [SerializeField] private Color selectedTint = new Color(0f, 1f, 0.651f, 1f);
    [SerializeField] private Color disabledTint = new Color(1f, 1f, 1f, 0.28f);

    private readonly Dictionary<Button, Color> baseColors = new Dictionary<Button, Color>();
    private GameManager subscribedManager = null;
    private CardGame subscribedGame = null;

    public DeclaredCombinationTier SelectedTier => selectedTier;
    public bool IsChoosing { get; private set; }

    public event Action<DeclaredCombinationTier> OnSelectedTierChanged;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        RefreshButtons();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RegisterButtonListeners();
        SubscribeToManager(gameManager);
        RefreshButtons();
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
        UnsubscribeFromManager();
        IsChoosing = false;
    }

    public void ToggleChoosing()
    {
        if (!CanLocalPlayerChoose())
        {
            IsChoosing = false;
            RefreshButtons();
            return;
        }

        IsChoosing = !IsChoosing;
        RefreshButtons();
    }

    public void SelectEasy()
    {
        TrySelectTier(DeclaredCombinationTier.Easy);
    }

    public void SelectMedium()
    {
        TrySelectTier(DeclaredCombinationTier.Medium);
    }

    public void SelectHard()
    {
        TrySelectTier(DeclaredCombinationTier.Hard);
    }

    public void SetSelectedTier(DeclaredCombinationTier tier)
    {
        SetSelectedTier(tier, true);
    }

    private void TrySelectTier(DeclaredCombinationTier tier)
    {
        if (requireToggleBeforeSelecting && !IsChoosing)
            return;

        if (!CanSelectTier(tier))
        {
            RefreshButtons();
            return;
        }

        SetSelectedTier(tier, true);

        if (closeAfterSelection)
            IsChoosing = false;

        RefreshButtons();
    }

    private void SetSelectedTier(DeclaredCombinationTier tier, bool notify)
    {
        DeclaredCombinationTier allowedTier = GetMinimumAllowedTier();
        if (tier < allowedTier)
            tier = allowedTier;

        if (selectedTier == tier)
        {
            RefreshButtons();
            return;
        }

        selectedTier = tier;
        if (notify)
            OnSelectedTierChanged?.Invoke(selectedTier);

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        ResolveReferences();

        bool canChoose = CanLocalPlayerChoose();
        if (!canChoose)
            IsChoosing = false;

        if (selectedTier < GetMinimumAllowedTier())
            selectedTier = GetMinimumAllowedTier();

        SetButtonState(toggleButton, canChoose, false);
        SetTierButtonState(easyButton, DeclaredCombinationTier.Easy, canChoose);
        SetTierButtonState(mediumButton, DeclaredCombinationTier.Medium, canChoose);
        SetTierButtonState(hardButton, DeclaredCombinationTier.Hard, canChoose);
    }

    private void SetTierButtonState(Button button, DeclaredCombinationTier tier, bool canChoose)
    {
        bool tierAllowed = canChoose && CanSelectTier(tier);
        bool interactable = tierAllowed && (!requireToggleBeforeSelecting || IsChoosing);
        SetButtonState(button, interactable, selectedTier == tier);
    }

    private void SetButtonState(Button button, bool interactable, bool selected)
    {
        if (button == null)
            return;

        button.interactable = interactable;

        Graphic graphic = button.targetGraphic;
        if (graphic == null)
            return;

        if (!baseColors.ContainsKey(button))
            baseColors.Add(button, graphic.color);

        if (!interactable && !selected)
            graphic.color = disabledTint;
        else if (selected)
            graphic.color = selectedTint;
        else if (IsChoosing)
            graphic.color = choosingTint;
        else
            graphic.color = baseColors[button];
    }

    private bool CanSelectTier(DeclaredCombinationTier tier)
    {
        if (!CanLocalPlayerChoose())
            return false;

        return tier >= GetMinimumAllowedTier();
    }

    private bool CanLocalPlayerChoose()
    {
        if (!TryGetContext(out CardGameRound round, out Skeleton localPlayer))
            return false;

        CardGame game = gameManager != null ? gameManager.CardGame : null;
        return game != null
            && game.phase == CardGame.GamePhase.Betting
            && round.CurrentPlayer == localPlayer
            && round.ActivePlayers.Contains(localPlayer);
    }

    private DeclaredCombinationTier GetMinimumAllowedTier()
    {
        if (!TryGetContext(out CardGameRound round, out Skeleton localPlayer))
            return DeclaredCombinationTier.Easy;

        if (round.playerStates.TryGetValue(localPlayer, out PlayerBetState state) && state.declaredTarget != null)
            return state.declaredTarget.Value;

        return DeclaredCombinationTier.Easy;
    }

    private bool TryGetContext(out CardGameRound round, out Skeleton localPlayer)
    {
        round = null;
        localPlayer = null;

        if (gameManager == null || gameManager.CardGame == null || gameManager.CardGame.round == null || gameManager.LocalPlayer == null)
            return false;

        round = gameManager.CardGame.round;
        localPlayer = gameManager.LocalPlayer;
        return true;
    }

    private void RegisterButtonListeners()
    {
        Register(toggleButton, ToggleChoosing);
        Register(easyButton, SelectEasy);
        Register(mediumButton, SelectMedium);
        Register(hardButton, SelectHard);
    }

    private void UnregisterButtonListeners()
    {
        Unregister(toggleButton, ToggleChoosing);
        Unregister(easyButton, SelectEasy);
        Unregister(mediumButton, SelectMedium);
        Unregister(hardButton, SelectHard);
    }

    private static void Register(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void Unregister(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (toggleButton == null)
        {
            GameObject turnMenu = GameObject.Find("TurnMenu");
            Transform newButton = turnMenu != null ? FindChildRecursive(turnMenu.transform, "B5") : null;
            toggleButton = newButton != null ? newButton.GetComponent<Button>() : null;
        }

        if (easyButton == null)
            easyButton = ResolveOptionButton("easy");
        if (mediumButton == null)
            mediumButton = ResolveOptionButton("medium");
        if (hardButton == null)
            hardButton = ResolveOptionButton("hard");
    }

    private Button ResolveOptionButton(string buttonName)
    {
        Transform option = FindChildRecursive(transform, buttonName);
        if (option == null)
            return null;

        Button button = option.GetComponent<Button>();
        if (button == null)
            button = option.gameObject.AddComponent<Button>();

        if (button.targetGraphic == null)
            button.targetGraphic = option.GetComponent<Graphic>();

        return button;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (string.Equals(root.name, childName, StringComparison.OrdinalIgnoreCase))
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
        subscribedManager.OnCardDealCompleted += HandleCardDealCompleted;
        SubscribeToGame(subscribedManager.CardGame);
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnGameCreated -= HandleGameCreated;
        subscribedManager.OnCardDealCompleted -= HandleCardDealCompleted;
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
        subscribedGame.OnTargetDeclared += HandleTargetChanged;
        subscribedGame.OnTargetUpgraded += HandleTargetChanged;
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
        subscribedGame.OnTargetDeclared -= HandleTargetChanged;
        subscribedGame.OnTargetUpgraded -= HandleTargetChanged;
        subscribedGame.OnTurnStarted -= HandlePlayerChanged;
        subscribedGame.OnTurnEnded -= HandlePlayerChanged;
        subscribedGame = null;
    }

    private void HandleGameCreated(CardGame game)
    {
        SubscribeToGame(game);
        RefreshButtons();
    }

    private void HandleCardDealCompleted()
    {
        RefreshButtons();
    }

    private void HandlePhaseChanged(CardGame.GamePhase phase)
    {
        RefreshButtons();
    }

    private void HandleRoundChanged(CardGameRound round)
    {
        RefreshButtons();
    }

    private void HandleTargetChanged(Skeleton player, DeclaredCombinationTier tier)
    {
        RefreshButtons();
    }

    private void HandlePlayerChanged(Skeleton player)
    {
        RefreshButtons();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
        RefreshButtons();
    }
#endif
}
