using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using CardGameRound = CardGame.Round;

[AddComponentMenu("UI/Turn Action Buttons")]
[DisallowMultipleComponent]
public class TurnActionButtons : MonoBehaviour
{
    public event Action LocalPlayerTurnStarted;
    public event Action LocalPlayerTurnEnded;

    [Header("Game")]
    [SerializeField] private GameManager gameManager = null;

    [Header("Buttons")]
    [SerializeField] private Button skipButton = null;
    [SerializeField] private Button takeButton = null;
    [SerializeField] private Button passButton = null;
    [SerializeField] private bool registerButtonClicks = true;
    [SerializeField] private Canvas rootCanvas = null;
    [SerializeField] private GraphicRaycaster graphicRaycaster = null;
    [SerializeField] private CanvasGroup canvasGroup = null;

    private GameManager subscribedManager = null;
    private CardGame subscribedGame = null;

    public bool IsVisible
    {
        get
        {
            bool canvasVisible = rootCanvas == null || rootCanvas.enabled;
            bool groupVisible = canvasGroup == null || canvasGroup.alpha > 0.001f;
            return isActiveAndEnabled && canvasVisible && groupVisible;
        }
    }

    public bool CanShowForLocalPlayer
    {
        get
        {
            return TryGetActionContext(out CardGameRound round, out Skeleton localPlayer)
                && CanLocalPlayerAct(round, localPlayer);
        }
    }

    private void Reset()
    {
        ResolveSceneReferences();
    }

    private void Awake()
    {
        ResolveSceneReferences();
    }

    private void OnEnable()
    {
        ResolveSceneReferences();
        RegisterButtonListeners();
        SubscribeToManager(gameManager);
        RefreshButtons();
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
        UnsubscribeFromManager();
        SetButtonsInteractable(false, false, false);
    }

    public void RefreshButtons()
    {
        if (!TryGetActionContext(out CardGameRound round, out Skeleton localPlayer))
        {
            SetButtonsInteractable(false, false, false);
            return;
        }

        bool canSkip = CanSkipTurn(round, localPlayer);
        bool canTake = CanTakeCard(round, localPlayer);
        bool canPass = CanPass(round, localPlayer);

        SetButtonsInteractable(canSkip, canTake, canPass);
    }

    public void Show()
    {
        ResolveCanvasReferences();

        if (rootCanvas != null)
            rootCanvas.enabled = true;

        if (graphicRaycaster != null)
            graphicRaycaster.enabled = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        RefreshButtons();
    }

    public void Hide()
    {
        ResolveCanvasReferences();

        if (rootCanvas != null)
            rootCanvas.enabled = false;

        if (graphicRaycaster != null)
            graphicRaycaster.enabled = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        SetButtonsInteractable(false, false, false);
    }

    public void SkipTurn()
    {
        if (gameManager != null && gameManager.IsNetworkMode)
        {
            if (!TryGetActionContext(out _, out _)) { RefreshButtons(); return; }
            gameManager.RequestSkipTurn();
            RefreshButtons();
        }
        else
        {
            TryRunAction("skip turn", CanSkipTurn, (round, localPlayer) => round.EndTurn(localPlayer));
        }
    }

    public void TakeCard()
    {
        if (gameManager != null && gameManager.IsNetworkMode)
        {
            if (!TryGetActionContext(out _, out _)) { RefreshButtons(); return; }
            gameManager.RequestTakeCard();
            RefreshButtons();
        }
        else
        {
            TryRunAction("take card", CanTakeCard, (round, localPlayer) => {
                round.TakeCard(localPlayer);
                if (round.HasMatchedBet(localPlayer))
                    round.EndTurn(localPlayer);
                });
        }
    }

    public void Pass()
    {
        if (gameManager != null && gameManager.IsNetworkMode)
        {
            if (!TryGetActionContext(out _, out _)) { RefreshButtons(); return; }
            gameManager.RequestFold();
            RefreshButtons();
        }
        else
        {
            TryRunAction("pass", CanPass, (round, localPlayer) => round.Fold(localPlayer));
        }
    }

    private void TryRunAction(string actionName, Func<CardGameRound, Skeleton, bool> canRun, Action<CardGameRound, Skeleton> action)
    {
        if (!TryGetActionContext(out CardGameRound round, out Skeleton localPlayer))
        {
            Debug.LogWarning($"{nameof(TurnActionButtons)} on {name} cannot {actionName}: no active local turn.", this);
            RefreshButtons();
            return;
        }

        if (!canRun(round, localPlayer))
        {
            RefreshButtons();
            return;
        }

        try
        {
            action(round, localPlayer);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(TurnActionButtons)} on {name} failed to {actionName}: {exception.Message}", this);
        }

        RefreshButtons();
    }

    private bool TryGetActionContext(out CardGameRound round, out Skeleton localPlayer)
    {
        round = null;
        localPlayer = null;

        if (gameManager == null || gameManager.CardGame == null || gameManager.LocalPlayer == null)
            return false;

        round = gameManager.CardGame.round;
        localPlayer = gameManager.LocalPlayer;
        return round != null;
    }

    private bool CanSkipTurn(CardGameRound round, Skeleton localPlayer)
    {
        return CanLocalPlayerAct(round, localPlayer)
            && round.HasMatchedBet(localPlayer);
    }

    private bool CanTakeCard(CardGameRound round, Skeleton localPlayer)
    {
        return CanLocalPlayerAct(round, localPlayer)
            && round.HasMatchedBet(localPlayer)
            && !gameManager.IsCardDealInProgress
            && round.CanTakeCard(localPlayer);
    }

    private bool CanPass(CardGameRound round, Skeleton localPlayer)
    {
        return CanLocalPlayerAct(round, localPlayer)
            && !round.HasMatchedBet(localPlayer)
            && round.ActivePlayers.Count > 1;
    }

    private bool CanLocalPlayerAct(CardGameRound round, Skeleton localPlayer)
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
        subscribedGame.OnPriceMatched += HandlePriceChanged;
        subscribedGame.OnPriceRaised += HandlePriceChanged;
        subscribedGame.OnPlayerFolded += HandlePlayerChanged;
        subscribedGame.OnTurnStarted += HandleTurnStarted;
        subscribedGame.OnTurnEnded += HandleTurnEnded;
        subscribedGame.OnCardTaken += HandleCardTaken;
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
        subscribedGame.OnRoundEnded -= HandleRoundEnded;
        subscribedGame.OnPriceMatched -= HandlePriceChanged;
        subscribedGame.OnPriceRaised -= HandlePriceChanged;
        subscribedGame.OnPlayerFolded -= HandlePlayerChanged;
        subscribedGame.OnTurnStarted -= HandleTurnStarted;
        subscribedGame.OnTurnEnded -= HandleTurnEnded;
        subscribedGame.OnCardTaken -= HandleCardTaken;
        subscribedGame.OnTableCardsDealt -= HandleTableCardsDealt;
        subscribedGame = null;
    }

    private void RegisterButtonListeners()
    {
        if (!registerButtonClicks)
            return;

        AddButtonListener(skipButton, SkipTurn);
        AddButtonListener(takeButton, TakeCard);
        AddButtonListener(passButton, Pass);
    }

    private void UnregisterButtonListeners()
    {
        RemoveButtonListener(skipButton, SkipTurn);
        RemoveButtonListener(takeButton, TakeCard);
        RemoveButtonListener(passButton, Pass);
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
    }

    private void SetButtonsInteractable(bool canSkip, bool canTake, bool canPass)
    {
        SetButtonInteractable(skipButton, canSkip);
        SetButtonInteractable(takeButton, canTake);
        SetButtonInteractable(passButton, canPass);
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
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

    private void HandleRoundEnded(RoundResult result)
    {
        RefreshButtons();
    }

    private void HandlePriceChanged(Skeleton player, int price)
    {
        RefreshButtons();
    }

    private void HandlePlayerChanged(Skeleton player)
    {
        RefreshButtons();
    }

    private void HandleTurnStarted(Skeleton player)
    {
        RefreshButtons();

        if (player == gameManager.LocalPlayer && CanShowForLocalPlayer)
            LocalPlayerTurnStarted?.Invoke();
    }

    private void HandleTurnEnded(Skeleton player)
    {
        RefreshButtons();

        if (player == gameManager.LocalPlayer)
            LocalPlayerTurnEnded?.Invoke();
    }

    private void HandleCardTaken(Skeleton player, CardData card)
    {
        RefreshButtons();
    }

    private void HandleTableCardsDealt(IReadOnlyList<CardData> cards)
    {
        RefreshButtons();
    }

    private void ResolveSceneReferences()
    {
        ResolveCanvasReferences();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (skipButton == null)
            skipButton = FindChildButton("B3");
        if (takeButton == null)
            takeButton = FindChildButton("B2");
        if (passButton == null)
            passButton = FindChildButton("B4");
    }

    private void ResolveCanvasReferences()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponent<Canvas>();

        if (graphicRaycaster == null)
            graphicRaycaster = GetComponent<GraphicRaycaster>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private Button FindChildButton(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveSceneReferences();
    }
#endif
}
