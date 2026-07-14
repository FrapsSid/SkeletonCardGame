using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Multiplayer;

using CardGameRound = CardGame.Round;

[AddComponentMenu("UI/Betting Canvas UI")]
[DisallowMultipleComponent]
public class BettingCanvasUI : MonoBehaviour
{
    private struct PartButtonBinding
    {
        public Button Button;
        public BodyPartType PartType;
    }

    private struct TierButtonBinding
    {
        public Button Button;
        public DeclaredCombinationTier Tier;
    }

    [Header("Game")]
    [SerializeField] private GameManager gameManager = null;

    [Header("UI State")]
    [SerializeField] private UIStateController uiStateController = null;

    [Header("Canvases")]
    [SerializeField] private Canvas bettingCanvas = null;
    [SerializeField] private GraphicRaycaster bettingRaycaster = null;
    [SerializeField] private CanvasGroup bettingCanvasGroup = null;

    [Header("Buttons")]
    [SerializeField] private Button openButton = null;

    [Header("Bet Info Display")]
    [SerializeField] private TMP_Text currentBetText;
    [SerializeField] private TMP_Text matchAmountText;
    [SerializeField] private TMP_Text totalPotText;

    [Header("State")]
    [SerializeField] private bool closeOnStart = true;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0f, 1f, 0.651f, 1f);
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.28f);
    [SerializeField] private Color invalidColor = new Color(0.55f, 0.55f, 0.55f, 0.35f);

    private readonly List<Button> backButtons = new List<Button>();
    private readonly List<Button> submitButtons = new List<Button>();
    private readonly List<PartButtonBinding> partButtons = new List<PartButtonBinding>();
    private readonly List<TierButtonBinding> tierButtons = new List<TierButtonBinding>();
    private readonly HashSet<BodyPartType> selectedParts = new HashSet<BodyPartType>();
    private readonly Dictionary<string, BodyPartType> bodyPartNameMap = new Dictionary<string, BodyPartType>(StringComparer.OrdinalIgnoreCase)
    {
        { "skull", BodyPartType.Head },
        { "ribcage", BodyPartType.Torso },
        { "hips", BodyPartType.Soul },
        { "rhand", BodyPartType.RightArm },
        { "lhand", BodyPartType.LeftArm },
        { "rleg", BodyPartType.RightLeg },
        { "lleg", BodyPartType.LeftLeg }
    };

    private DeclaredCombinationTier selectedTier = DeclaredCombinationTier.Easy;
    private GameManager subscribedManager = null;
    private CardGame subscribedGame = null;
    private SkeletonBody subscribedBody = null;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Reset()
    {
        ResolveSceneReferences();
    }

    private void Awake()
    {
        ResolveSceneReferences();
        if (closeOnStart)
            SetOpen(false);
    }

    private void OnEnable()
    {
        ResolveSceneReferences();
        RegisterButtonListeners();
        SubscribeToManager(gameManager);
        RefreshControls();

        var ngs = Multiplayer.NetworkGameState.Instance;
        if (ngs != null)
            ngs.OnParticipationPriceChanged += HandleNetworkPriceChanged;
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
        UnsubscribeFromManager();
        SubscribeToBody(null);

        var ngs = Multiplayer.NetworkGameState.Instance;
        if (ngs != null)
            ngs.OnParticipationPriceChanged -= HandleNetworkPriceChanged;
    }

    public void Open()
    {
        uiStateController.OpenBetting();
    }

    public void Close()
    {
        uiStateController.CloseBetting();
    }

    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    public void Show()
    {
        LoadExistingBetSelection();
        SetOpen(true);
        RefreshControls();
    }

    public void Hide()
    {
        SetOpen(false);
    }

    public void SelectTier(DeclaredCombinationTier tier)
    {
        selectedTier = tier;
        RefreshControls();
    }

    public void ToggleBodyPartSelection(BodyPartType partType)
    {
        if (!CanChooseBodyPart(partType))
        {
            selectedParts.Remove(partType);
            RefreshControls();
            return;
        }

        if (!selectedParts.Add(partType))
            selectedParts.Remove(partType);

        RefreshControls();
    }

    public bool CanDragBodyPart(BodyPartType partType)
    {
        return isOpen && CanChooseBodyPart(partType);
    }

    public bool DropBodyPartOnTier(BodyPartType partType, DeclaredCombinationTier tier)
    {
        if (!CanChooseBodyPart(partType))
        {
            selectedParts.Remove(partType);
            RefreshControls();
            return false;
        }

        selectedTier = tier;
        selectedParts.Add(partType);
        RefreshControls();
        return true;
    }

    public void SubmitBet()
    {
        if (!TryGetBettingContext(out CardGameRound round, out Skeleton localPlayer))
        {
            RefreshControls();
            return;
        }

        List<StakeAsset> assets = BuildSelectedAssets();
        if (!CanSubmitBet(round, localPlayer, assets))
        {
            RefreshControls();
            return;
        }

        try
        {
            if (gameManager != null && gameManager.IsNetworkMode)
            {
                gameManager.RequestBet(new List<BodyPartType>(selectedParts), selectedTier);
            }
            else
            {
                int selectedValue = CalculateStakeValue(assets);
                if (selectedValue > round.currentParticipationPrice && round.CanRaise(localPlayer, assets, selectedTier))
                    round.Raise(localPlayer, assets, selectedTier);
                else
                    round.Call(localPlayer, assets, selectedTier);
            }

            Close();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(BettingCanvasUI)} on {name} could not submit bet: {exception.Message}", this);
        }

        RefreshControls();
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        ResolveCanvasReferences(true);

        if (bettingCanvas != null)
            bettingCanvas.enabled = open;

        if (bettingRaycaster != null)
            bettingRaycaster.enabled = open;

        if (bettingCanvasGroup != null)
        {
            bettingCanvasGroup.alpha = open ? 1f : 0f;
            bettingCanvasGroup.interactable = open;
            bettingCanvasGroup.blocksRaycasts = open;
        }
    }

    private void RefreshControls()
    {
        SubscribeToBody(gameManager != null && gameManager.LocalPlayer != null ? gameManager.LocalPlayer.Body : null);

        bool hasContext = TryGetBettingContext(out CardGameRound round, out Skeleton localPlayer);
        List<StakeAsset> selectedAssets = BuildSelectedAssets();
        bool canSubmit = hasContext && CanSubmitBet(round, localPlayer, selectedAssets);

        SetButtonsInteractable(backButtons, true);
        SetButtonsInteractable(submitButtons, canSubmit);

        RefreshTierButtons(round, localPlayer, hasContext);
        RefreshBodyPartButtons(hasContext);
        RefreshBetInfo(round, localPlayer, hasContext);
    }

    private void RefreshBetInfo(CardGameRound round, Skeleton localPlayer, bool hasContext)
    {
        int selectedValue = CalculateSelectedValue();
        int needAmount = 0;
        int totalPot = 0;

        if (hasContext && round != null)
        {
            int currentPrice = round.currentParticipationPrice;
            if (gameManager != null && gameManager.IsNetworkMode)
            {
                var ngs = Multiplayer.NetworkGameState.Instance;
                if (ngs != null)
                    currentPrice = ngs.CurrentParticipationPrice;
            }

            if (localPlayer != null && round.playerStates.TryGetValue(localPlayer, out PlayerBetState state))
            {
                needAmount = Mathf.Max(0, currentPrice - state.committedValue);
            }
            else
            {
                needAmount = currentPrice;
            }

            totalPot = CalculateTotalPot(round);
        }

        SetText(currentBetText, $"Your Bet: {selectedValue}");
        SetText(matchAmountText, needAmount > 0 ? $"Need: {needAmount}" : "Ready");
        SetText(totalPotText, $"Pot: {totalPot}");
    }

    private int CalculateSelectedValue()
    {
        int value = 0;
        foreach (BodyPartType partType in selectedParts)
        {
            value += partType.BodyPartCost();
        }
        return value;
    }

    private static int CalculateTotalPot(CardGameRound round)
    {
        int total = 0;
        foreach (var state in round.playerStates.Values)
        {
            total += state.committedValue;
        }
        return total;
    }

    private static void SetText(TMP_Text textComponent, string value)
    {
        if (textComponent == null) return;
        textComponent.text = value;
        textComponent.gameObject.SetActive(true);
    }

    private void RefreshTierButtons(CardGameRound round, Skeleton localPlayer, bool hasContext)
    {
        foreach (TierButtonBinding binding in tierButtons)
        {
            bool tierAllowed = true;
            if (hasContext && round.playerStates.TryGetValue(localPlayer, out PlayerBetState state) && state.declaredTarget != null)
                tierAllowed = binding.Tier >= state.declaredTarget.Value;

            SetButtonState(binding.Button, tierAllowed, binding.Tier == selectedTier);
        }
    }

    private void RefreshBodyPartButtons(bool hasContext)
    {
        foreach (PartButtonBinding binding in partButtons)
        {
            bool available = hasContext && TryGetAvailableAsset(binding.PartType, out _);
            if (!available)
                selectedParts.Remove(binding.PartType);

            SetButtonState(binding.Button, available, selectedParts.Contains(binding.PartType));
        }
    }

    private void SetButtonState(Button button, bool interactable, bool selected)
    {
        if (button == null)
            return;

        button.interactable = interactable;
        Color targetColor = interactable ? (selected ? selectedColor : normalColor) : disabledColor;
        if (!interactable && selected)
            targetColor = invalidColor;

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic != null)
            targetGraphic.color = targetColor;
    }

    private static void SetButtonsInteractable(List<Button> buttons, bool interactable)
    {
        foreach (Button button in buttons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    private void LoadExistingBetSelection()
    {
        selectedParts.Clear();

        if (!TryGetBettingContext(out CardGameRound round, out Skeleton localPlayer))
            return;

        if (!round.playerStates.TryGetValue(localPlayer, out PlayerBetState state))
            return;

        if (state.declaredTarget != null)
            selectedTier = state.declaredTarget.Value;

        foreach (StakeAsset asset in state.committedAssets)
        {
            if (asset != null && asset.bodyPart != null && IsLocalBodyAsset(asset))
                selectedParts.Add(asset.bodyPart.Item.Type);
        }
    }

    private bool CanSubmitBet(CardGameRound round, Skeleton localPlayer, List<StakeAsset> assets)
    {
        if (!CanLocalPlayerBet(round, localPlayer))
            return false;

        int selectedValue = CalculateStakeValue(assets);
        if (selectedValue > round.currentParticipationPrice)
            return round.CanRaise(localPlayer, assets, selectedTier);

        return round.CanCall(localPlayer, assets, selectedTier);
    }

    private bool CanChooseBodyPart(BodyPartType partType)
    {
        if (!TryGetBettingContext(out CardGameRound round, out Skeleton localPlayer))
            return false;

        return CanLocalPlayerBet(round, localPlayer) && TryGetAvailableAsset(partType, out _);
    }

    private bool CanLocalPlayerBet(CardGameRound round, Skeleton localPlayer)
    {
        CardGame game = gameManager != null ? gameManager.CardGame : null;
        return game != null
            && game.phase == CardGame.GamePhase.Betting
            && round != null
            && localPlayer != null
            && round.CurrentPlayer == localPlayer
            && round.ActivePlayers.Contains(localPlayer)
            && round.playerStates.ContainsKey(localPlayer)
            && round.playerTurnStates.ContainsKey(localPlayer);
    }

    private bool TryGetBettingContext(out CardGameRound round, out Skeleton localPlayer)
    {
        round = null;
        localPlayer = null;

        if (gameManager == null || gameManager.CardGame == null || gameManager.LocalPlayer == null)
            return false;

        round = gameManager.CardGame.round;
        localPlayer = gameManager.LocalPlayer;
        return round != null;
    }

    private List<StakeAsset> BuildSelectedAssets()
    {
        List<StakeAsset> assets = new List<StakeAsset>();
        List<BodyPartType> missingParts = null;

        foreach (BodyPartType partType in selectedParts)
        {
            if (TryGetAvailableAsset(partType, out StakeAsset asset))
                assets.Add(asset);
            else
            {
                if (missingParts == null)
                    missingParts = new List<BodyPartType>();
                missingParts.Add(partType);
            }
        }

        if (missingParts != null)
        {
            foreach (BodyPartType partType in missingParts)
                selectedParts.Remove(partType);
        }

        return assets;
    }

    private bool TryGetAvailableAsset(BodyPartType partType, out StakeAsset foundAsset)
    {
        foundAsset = null;

        if (gameManager == null || gameManager.LocalPlayer == null || gameManager.LocalPlayer.team == null)
            return false;

        Team localTeam = gameManager.LocalPlayer.team;
        foreach (StakeAsset asset in localTeam.Assets)
        {
            if (asset == null || asset.bodyPart == null || asset.bodyPart.Item.Type != partType)
                continue;

            if (!IsLocalBodyAsset(asset))
                continue;

            if (asset.bodyPart.State != BodyPartState.Attached || asset.bodyPart.currentHolder == null)
                continue;

            foundAsset = asset;
            return true;
        }

        return false;
    }

    private bool IsLocalBodyAsset(StakeAsset asset)
    {
        if (asset == null || gameManager == null || gameManager.LocalPlayer == null)
            return false;

        Skeleton localPlayer = gameManager.LocalPlayer;
        if (asset.owningTeam != localPlayer.team)
            return false;

        if (asset.sourceOwner != null)
            return asset.sourceOwner == localPlayer;

        return localPlayer.Body != null
            && asset.bodyPart != null
            && asset.bodyPart.currentHolder == localPlayer.Body.gameObject;
    }

    private static int CalculateStakeValue(IList<StakeAsset> assets)
    {
        int value = 0;
        foreach (StakeAsset asset in assets)
        {
            if (asset != null)
                value += asset.stakeValue;
        }

        return value;
    }

    private void RegisterButtonListeners()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(Open);
            openButton.onClick.AddListener(Open);
        }

        foreach (Button button in backButtons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveListener(Close);
            button.onClick.AddListener(Close);
        }

        foreach (Button button in submitButtons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveListener(SubmitBet);
            button.onClick.AddListener(SubmitBet);
        }
    }

    private void UnregisterButtonListeners()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(Open);

        foreach (Button button in backButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(Close);
        }

        foreach (Button button in submitButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(SubmitBet);
        }
    }

    private void ResolveSceneReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        ResolveCanvasReferences(true);
        ResolveButtons();
    }

    private void ResolveCanvasReferences(bool addMissingCanvasGroup)
    {
        if (bettingCanvas == null)
            bettingCanvas = GetComponent<Canvas>();

        if (bettingRaycaster == null)
            bettingRaycaster = GetComponent<GraphicRaycaster>();

        if (bettingCanvasGroup == null)
            bettingCanvasGroup = GetComponent<CanvasGroup>();

        if (bettingCanvasGroup == null && addMissingCanvasGroup)
            bettingCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void ResolveButtons()
    {
        backButtons.Clear();
        submitButtons.Clear();
        partButtons.Clear();
        tierButtons.Clear();

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            string buttonName = button.gameObject.name;
            if (ContainsName(buttonName, "back"))
            {
                AddUnique(backButtons, button);
                continue;
            }

            if (ContainsName(buttonName, "ready"))
            {
                AddUnique(submitButtons, button);
                continue;
            }

            if (TryGetPartType(buttonName, out BodyPartType partType))
            {
                partButtons.Add(new PartButtonBinding { Button = button, PartType = partType });
                BettingBodyPartButton relay = button.GetComponent<BettingBodyPartButton>();
                if (relay == null)
                    relay = button.gameObject.AddComponent<BettingBodyPartButton>();
                relay.Configure(this, partType);
                continue;
            }

            if (TryGetTier(buttonName, out DeclaredCombinationTier tier))
            {
                tierButtons.Add(new TierButtonBinding { Button = button, Tier = tier });
                BettingTierDropTarget relay = button.GetComponent<BettingTierDropTarget>();
                if (relay == null)
                    relay = button.gameObject.AddComponent<BettingTierDropTarget>();
                relay.Configure(this, tier);
            }
        }
    }

    private bool TryGetPartType(string buttonName, out BodyPartType partType)
    {
        return bodyPartNameMap.TryGetValue(buttonName, out partType);
    }

    private static bool TryGetTier(string buttonName, out DeclaredCombinationTier tier)
    {
        if (ContainsName(buttonName, "easy"))
        {
            tier = DeclaredCombinationTier.Easy;
            return true;
        }

        if (ContainsName(buttonName, "medium"))
        {
            tier = DeclaredCombinationTier.Medium;
            return true;
        }

        if (ContainsName(buttonName, "hard"))
        {
            tier = DeclaredCombinationTier.Hard;
            return true;
        }

        tier = DeclaredCombinationTier.Easy;
        return false;
    }

    private static bool ContainsName(string value, string token)
    {
        return value != null && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void AddUnique(List<Button> buttons, Button button)
    {
        if (button != null && !buttons.Contains(button))
            buttons.Add(button);
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
        subscribedGame.OnRoundEnded += HandleRoundEnded;
        subscribedGame.OnTargetDeclared += HandleTargetChanged;
        subscribedGame.OnTargetUpgraded += HandleTargetChanged;
        subscribedGame.OnPriceMatched += HandlePriceChanged;
        subscribedGame.OnPriceRaised += HandlePriceChanged;
        subscribedGame.OnPlayerFolded += HandlePlayerChanged;
        subscribedGame.OnTurnStarted += HandlePlayerChanged;
        subscribedGame.OnTurnEnded += HandlePlayerChanged;
        subscribedGame.OnPotResolved += HandlePotResolved;
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
        subscribedGame.OnTargetDeclared -= HandleTargetChanged;
        subscribedGame.OnTargetUpgraded -= HandleTargetChanged;
        subscribedGame.OnPriceMatched -= HandlePriceChanged;
        subscribedGame.OnPriceRaised -= HandlePriceChanged;
        subscribedGame.OnPlayerFolded -= HandlePlayerChanged;
        subscribedGame.OnTurnStarted -= HandlePlayerChanged;
        subscribedGame.OnTurnEnded -= HandlePlayerChanged;
        subscribedGame.OnPotResolved -= HandlePotResolved;
        subscribedGame = null;
    }

    private void SubscribeToBody(SkeletonBody body)
    {
        if (subscribedBody == body)
            return;

        if (subscribedBody != null)
            subscribedBody.OnBodyChanged -= HandleBodyChanged;

        subscribedBody = body;
        if (subscribedBody != null)
            subscribedBody.OnBodyChanged += HandleBodyChanged;
    }

    private void HandleGameCreated(CardGame game)
    {
        SubscribeToGame(game);
        RefreshControls();
    }

    private void HandleCardDealCompleted()
    {
        RefreshControls();
    }

    private void HandlePhaseChanged(CardGame.GamePhase phase)
    {
        RefreshControls();
    }

    private void HandleRoundChanged(CardGameRound round)
    {
        RefreshControls();
    }

    private void HandleRoundEnded(RoundResult result)
    {
        RefreshControls();
    }

    private void HandleTargetChanged(Skeleton player, DeclaredCombinationTier tier)
    {
        RefreshControls();
    }

    private void HandlePriceChanged(Skeleton player, int price)
    {
        RefreshControls();
    }

    private void HandlePlayerChanged(Skeleton player)
    {
        RefreshControls();
    }

    private void HandlePotResolved(IReadOnlyList<Team> winners, IReadOnlyList<StakeAsset> assets)
    {
        RefreshControls();
    }

    private void HandleBodyChanged()
    {
        RefreshControls();
    }

    private void HandleNetworkPriceChanged(int newPrice)
    {
        RefreshControls();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        ResolveCanvasReferences(false);
    }
#endif
}
