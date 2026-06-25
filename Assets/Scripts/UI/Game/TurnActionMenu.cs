using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using CardGameModel = CardGame;
using CardGameRound = CardGame.Round;

public sealed class TurnActionMenu : GameUIScreen
{
    public override ScreenId Id => ScreenId.TurnActionMenu;

    private RectTransform menuPanel;
    private TMP_Text indicatorText;
    private TMP_Text statusText;
    private Button betButton;
    private Button passButton;
    private Button takeCardButton;
    private Button skipButton;
    private Button hideButton;

    private GameManager subscribedManager;
    private CardGameModel subscribedGame;
    private Skeleton subscribedPlayer;
    private Skeleton currentPlayer;
    private bool menuHidden;
    private bool previewMode;

    protected override void Build()
    {
        RectTransform frame = GameUIFactory.Panel(transform, "HudFrame", new Color(0f, 0f, 0f, 0.08f), false);
        GameUIFactory.Stretch(frame, 10f, 10f, 10f, 10f);
        frame.GetComponent<Image>().raycastTarget = false;
        GameUIFactory.AddGlow(frame.gameObject, GameUITheme.CyanDeepGlow, GameUITheme.CyanDeepGlow, Vector2.zero);

        indicatorText = GameUIFactory.Text(transform, "TurnIndicator", "ТВОЙ ХОД", 32f, TextAlignmentOptions.Center, GameUITheme.Cyan, FontStyles.Bold);
        GameUIFactory.Anchor(indicatorText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(280f, 48f));

        menuPanel = GameUIFactory.CreateRect("ActionButtons", transform);
        GameUIFactory.Anchor(menuPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 28f), new Vector2(900f, 430f));

        skipButton = CreateActionButton("ПРОПУСТИТЬ", EndTurn, new Vector2(-300f, 92f));
        takeCardButton = CreateActionButton("ВЗЯТЬ КАРТУ", TakeCard, new Vector2(0f, 92f));
        betButton = CreateActionButton("СТАВКА", OpenBetScreen, new Vector2(300f, 92f));
        passButton = CreateActionButton("ПАС", Fold, new Vector2(-150f, -108f));
        hideButton = CreateActionButton("НОВАЯ ЦЕЛЬ", ToggleMenuHidden, new Vector2(150f, -108f));

        statusText = GameUIFactory.Text(transform, "Status", string.Empty, 20f, TextAlignmentOptions.Center, GameUITheme.Red, FontStyles.Bold);
        GameUIFactory.Anchor(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 68f), new Vector2(0f, 36f));
    }

    public void TickAlways()
    {
        EnsureSubscribed();

        if (previewMode)
        {
            if (!gameObject.activeSelf)
                UI.ShowHud(Id);
            Refresh();
            return;
        }

        SyncCurrentPlayerFromGameManager();

        bool shouldShow = IsCurrentHumanTurn()
            && UI.ActiveScreen == ScreenId.None;
        if (shouldShow && !gameObject.activeSelf)
            UI.ShowHud(Id);
        else if (!shouldShow && gameObject.activeSelf)
            UI.HideHud(Id);

        if (gameObject.activeSelf)
            Refresh();
    }

    public void SetPreviewMode(bool enabled)
    {
        previewMode = enabled;
        if (!previewMode && currentPlayer == null && gameObject.activeSelf)
            UI.HideHud(Id);
        Refresh();
    }

    public bool TryHandleEscape()
    {
        if (!gameObject.activeSelf || !menuHidden)
            return false;

        menuHidden = false;
        Refresh();
        return true;
    }

    protected override void OnShow()
    {
        Refresh();
    }

    private Button CreateActionButton(string label, UnityEngine.Events.UnityAction action, Vector2 position)
    {
        Button button = GameUIFactory.Button(menuPanel, label + " Button", label, action, false);
        GameUIFactory.Anchor(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(260f, 170f));
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.fontSize = 26f;
        return button;
    }

    private void EnsureSubscribed()
    {
        UI.RefreshGameManager();
        GameManager manager = UI.GameManager;
        CardGameModel game = manager != null ? manager.CardGame : null;
        Skeleton player = manager != null ? manager.LocalPlayer : null;
        if (manager == subscribedManager && game == subscribedGame && player == subscribedPlayer)
            return;

        Unsubscribe();
        subscribedManager = manager;
        subscribedGame = game;
        subscribedPlayer = player;
        if (subscribedManager != null)
            subscribedManager.OnCardDealCompleted += HandleCardDealCompleted;

        if (subscribedGame == null)
            return;

        if (subscribedPlayer != null)
        {
            if (subscribedGame.TurnStartedByPlayer.TryGetValue(subscribedPlayer, out CardGameModel.PlayerTurnEvent turnStarted))
                turnStarted.Fired += HandleTurnStarted;
            if (subscribedGame.TurnEndedByPlayer.TryGetValue(subscribedPlayer, out CardGameModel.PlayerTurnEvent turnEnded))
                turnEnded.Fired += HandleTurnEnded;
        }
        subscribedGame.OnPriceRaised += HandleBettingChanged;
        subscribedGame.OnPriceMatched += HandleBettingChanged;
        subscribedGame.OnPlayerFolded += HandlePlayerFolded;
        subscribedGame.OnTargetUpgraded += HandleTargetChanged;
        subscribedGame.OnTargetDeclared += HandleTargetChanged;
        subscribedGame.OnBettingRoundEnded += HandleBettingRoundEnded;
        SyncCurrentPlayerFromGameManager();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (subscribedManager != null)
            subscribedManager.OnCardDealCompleted -= HandleCardDealCompleted;

        if (subscribedGame == null)
        {
            subscribedManager = null;
            subscribedPlayer = null;
            return;
        }

        if (subscribedPlayer != null)
        {
            if (subscribedGame.TurnStartedByPlayer.TryGetValue(subscribedPlayer, out CardGameModel.PlayerTurnEvent turnStarted))
                turnStarted.Fired -= HandleTurnStarted;
            if (subscribedGame.TurnEndedByPlayer.TryGetValue(subscribedPlayer, out CardGameModel.PlayerTurnEvent turnEnded))
                turnEnded.Fired -= HandleTurnEnded;
        }
        subscribedGame.OnPriceRaised -= HandleBettingChanged;
        subscribedGame.OnPriceMatched -= HandleBettingChanged;
        subscribedGame.OnPlayerFolded -= HandlePlayerFolded;
        subscribedGame.OnTargetUpgraded -= HandleTargetChanged;
        subscribedGame.OnTargetDeclared -= HandleTargetChanged;
        subscribedGame.OnBettingRoundEnded -= HandleBettingRoundEnded;
        subscribedManager = null;
        subscribedGame = null;
        subscribedPlayer = null;
    }

    private void Refresh()
    {
        CardGameRound round = subscribedGame != null ? subscribedGame.round : null;
        bool hasTurn = previewMode || IsCurrentHumanTurn(round);

        indicatorText.gameObject.SetActive(hasTurn);
        menuPanel.gameObject.SetActive(hasTurn && !menuHidden);
        GameUIFactory.SetButtonText(hideButton, menuHidden ? "ПОКАЗАТЬ" : "НОВАЯ ЦЕЛЬ");

        if (!hasTurn)
            return;

        if (previewMode && (round == null || currentPlayer == null))
        {
            indicatorText.text = "ТВОЙ ХОД";
            statusText.text = "ПРЕВЬЮ";
            betButton.interactable = true;
            takeCardButton.interactable = false;
            passButton.interactable = false;
            skipButton.interactable = false;
            hideButton.interactable = true;
            return;
        }

        bool isCurrentHumanTurn = IsCurrentHumanTurn(round);
        bool canUseActions = isCurrentHumanTurn && !IsCardDealInProgress();
        bool betRaised = round.currentParticipationPrice > 0;
        bool hasMatched = round.HasMatchedBet(currentPlayer);

        betButton.interactable = canUseActions && CanOpenBetScreen(round);
        takeCardButton.interactable = canUseActions && round.CanTakeCard(currentPlayer);
        passButton.interactable = canUseActions && betRaised;
        skipButton.interactable = canUseActions && hasMatched;
    }

    private void HandleTurnStarted(Skeleton player)
    {
        currentPlayer = player;
        menuHidden = false;
        statusText.text = string.Empty;
        if (UI.ActiveScreen == ScreenId.None && !gameObject.activeSelf)
            UI.ShowHud(Id);
        Refresh();
    }

    private void HandleTurnEnded(Skeleton player)
    {
        if (currentPlayer == player)
            currentPlayer = null;
        if (gameObject.activeSelf)
            UI.HideHud(Id);
        Refresh();
    }

    private void HandleBettingChanged(Skeleton player, int price) => Refresh();
    private void HandleTargetChanged(Skeleton player, DeclaredCombinationTier tier) => Refresh();
    private void HandlePlayerFolded(Skeleton player) => Refresh();
    private void HandleCardDealCompleted() => Refresh();

    private void HandleBettingRoundEnded(CardGameRound round)
    {
        currentPlayer = null;
        Refresh();
    }

    private void OpenBetScreen()
    {
        CardGameRound round = subscribedGame != null ? subscribedGame.round : null;
        if (!CanUseCurrentTurn(round))
            return;

        if (!CanOpenBetScreen(round))
        {
            statusText.text = "Betting is not available for this turn.";
            Refresh();
            return;
        }

        statusText.text = string.Empty;
        UI.PushModal(ScreenId.BetScreen);
    }

    private void TakeCard()
    {
        TryRoundAction(round =>
        {
            if (!round.CanTakeCard(currentPlayer))
                throw new InvalidOperationException("Taking a card is not available for this turn.");

            round.TakeCard(currentPlayer);
        });
    }

    private void Fold()
    {
        TryRoundAction(round => round.Fold(currentPlayer));
    }

    private void EndTurn()
    {
        TryRoundAction(round =>
        {
            if (!round.HasMatchedBet(currentPlayer))
                throw new InvalidOperationException("Match the current price before ending the turn.");

            round.EndTurn(currentPlayer);
        });
    }

    private void ToggleMenuHidden()
    {
        menuHidden = !menuHidden;
        Refresh();
    }

    private void TryRoundAction(Action<CardGameRound> action)
    {
        CardGameRound round = subscribedGame != null ? subscribedGame.round : null;
        if (!CanUseCurrentTurn(round))
            return;

        if (IsCardDealInProgress())
        {
            statusText.text = "Wait for the card deal to finish.";
            Refresh();
            return;
        }

        try
        {
            action(round);
            statusText.text = string.Empty;
        }
        catch (Exception exception)
        {
            statusText.text = exception.Message;
        }

        Refresh();
    }

    private bool CanUseCurrentTurn(CardGameRound round)
    {
        if (round == null || currentPlayer == null)
            return false;

        if (!IsCurrentHumanTurn(round))
        {
            statusText.text = "Waiting for your turn.";
            Refresh();
            return false;
        }

        return true;
    }

    private bool CanOpenBetScreen(CardGameRound round)
    {
        if (round == null || currentPlayer == null || round.CurrentPlayer != currentPlayer)
            return false;

        List<StakeAsset> ownedAssets = GetOwnedAssets(currentPlayer);
        StakeAsset[] noAssets = Array.Empty<StakeAsset>();
        foreach (DeclaredCombinationTier tier in Enum.GetValues(typeof(DeclaredCombinationTier)))
        {
            if (round.CanCall(currentPlayer, noAssets, tier)
                || round.CanCall(currentPlayer, ownedAssets, tier)
                || round.CanRaise(currentPlayer, ownedAssets, tier))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCardDealInProgress()
    {
        return subscribedManager != null && subscribedManager.IsCardDealInProgress;
    }

    private static List<StakeAsset> GetOwnedAssets(Skeleton player)
    {
        var assets = new List<StakeAsset>();
        if (player == null || player.team == null)
            return assets;

        foreach (StakeAsset asset in player.team.Assets)
        {
            if (asset != null && player.team.OwnsAsset(asset))
                assets.Add(asset);
        }

        return assets;
    }

    private bool IsCurrentHumanTurn(CardGameRound round = null)
    {
        GameManager manager = UI.GameManager;
        if (manager == null || currentPlayer == null)
            return false;

        CardGameRound activeRound = round != null ? round : subscribedGame != null ? subscribedGame.round : null;
        return activeRound != null
            && ReferenceEquals(manager.LocalPlayer, currentPlayer)
            && ReferenceEquals(activeRound.CurrentPlayer, currentPlayer);
    }

    private void SyncCurrentPlayerFromGameManager()
    {
        GameManager manager = UI.GameManager;
        Skeleton player = manager != null ? manager.LocalPlayer : null;
        currentPlayer = player;
        Refresh();
    }
}
