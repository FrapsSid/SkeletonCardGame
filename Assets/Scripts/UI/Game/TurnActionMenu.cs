using System;
using Assets.Scripts.CardGame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using CardGameModel = Assets.Scripts.CardGame.CardGame;
using CardGameRound = Assets.Scripts.CardGame.CardGame.Round;

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

    private CardGameModel subscribedGame;
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

        bool shouldShow = currentPlayer != null && UI.ActiveScreen == ScreenId.None;
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
        CardGameModel game = UI.GameManager != null ? UI.GameManager.CardGame : null;
        if (game == subscribedGame)
            return;

        Unsubscribe();
        subscribedGame = game;
        if (subscribedGame == null)
            return;

        subscribedGame.OnTurnStarted += HandleTurnStarted;
        subscribedGame.OnTurnEnded += HandleTurnEnded;
        subscribedGame.OnPriceRaised += HandleBettingChanged;
        subscribedGame.OnPriceMatched += HandleBettingChanged;
        subscribedGame.OnPlayerFolded += HandlePlayerFolded;
        subscribedGame.OnTargetUpgraded += HandleTargetChanged;
        subscribedGame.OnTargetDeclared += HandleTargetChanged;
        subscribedGame.OnBettingRoundEnded += HandleBettingRoundEnded;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (subscribedGame == null)
            return;

        subscribedGame.OnTurnStarted -= HandleTurnStarted;
        subscribedGame.OnTurnEnded -= HandleTurnEnded;
        subscribedGame.OnPriceRaised -= HandleBettingChanged;
        subscribedGame.OnPriceMatched -= HandleBettingChanged;
        subscribedGame.OnPlayerFolded -= HandlePlayerFolded;
        subscribedGame.OnTargetUpgraded -= HandleTargetChanged;
        subscribedGame.OnTargetDeclared -= HandleTargetChanged;
        subscribedGame.OnBettingRoundEnded -= HandleBettingRoundEnded;
        subscribedGame = null;
    }

    private void Refresh()
    {
        CardGameRound round = subscribedGame != null ? subscribedGame.round : null;
        bool hasTurn = previewMode || (round != null && currentPlayer != null);

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

        bool betRaised = round.currentParticipationPrice > 0;
        bool hasMatched = round.HasMatchedBet(currentPlayer);

        betButton.interactable = round.CanRaise(currentPlayer);
        takeCardButton.interactable = round.CanTakeCard(currentPlayer);
        passButton.interactable = betRaised;
        skipButton.interactable = !betRaised && hasMatched;
    }

    private void HandleTurnStarted(Skeleton player)
    {
        currentPlayer = player;
        menuHidden = false;
        statusText.text = string.Empty;
        Refresh();
    }

    private void HandleTurnEnded(Skeleton player)
    {
        if (currentPlayer == player)
            currentPlayer = null;
        Refresh();
    }

    private void HandleBettingChanged(Skeleton player, int price) => Refresh();
    private void HandleTargetChanged(Skeleton player, DeclaredCombinationTier tier) => Refresh();
    private void HandlePlayerFolded(Skeleton player) => Refresh();

    private void HandleBettingRoundEnded(CardGameRound round)
    {
        currentPlayer = null;
        Refresh();
    }

    private void OpenBetScreen()
    {
        statusText.text = string.Empty;
        UI.PushModal(ScreenId.BetScreen);
    }

    private void TakeCard()
    {
        TryRoundAction(round => round.TakeCard(currentPlayer));
    }

    private void Fold()
    {
        TryRoundAction(round => round.Fold(currentPlayer));
    }

    private void EndTurn()
    {
        TryRoundAction(round => round.EndTurn(currentPlayer));
    }

    private void ToggleMenuHidden()
    {
        menuHidden = !menuHidden;
        Refresh();
    }

    private void TryRoundAction(Action<CardGameRound> action)
    {
        CardGameRound round = subscribedGame != null ? subscribedGame.round : null;
        if (round == null || currentPlayer == null)
            return;

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
}
