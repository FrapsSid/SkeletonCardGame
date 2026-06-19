#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CardGameModel = Assets.Scripts.CardGame.CardGame;
using CardGameRound = Assets.Scripts.CardGame.CardGame.Round;
using GamePhase = Assets.Scripts.CardGame.CardGame.GamePhase;

public enum CardGameTurnAction
{
    Call,
    Raise,
    AllIn,
    TakeCard,
    Fold,
    EndTurn
}

public sealed class GameManager : MonoBehaviour
{
    [Serializable]
    private sealed class TeamSetup
    {
        public string teamName = "Team";
        public List<StakeAsset> assets = new List<StakeAsset>();
    }

    [Serializable]
    private sealed class PlayerSetup
    {
        public string playerName = "Player";
        public int teamIndex = 0;
    }

    [Header("Startup")]
    [SerializeField]
    private bool enableOnStart = true;

    [SerializeField]
    private bool createTestSetupWhenEmpty = true;

    [SerializeField]
    private bool createTestTurnUiWhenMissing = true;

    [Header("Players")]
    [SerializeField]
    private List<TeamSetup> teamSetups = new List<TeamSetup>();

    [SerializeField]
    private List<PlayerSetup> playerSetups = new List<PlayerSetup>();

    [SerializeField]
    private int currentPlayerIndex;

    [Header("Turn UI")]
    [SerializeField]
    private GameObject? turnUiObject;

    [SerializeField]
    private TurnUI? turnUi;

    [SerializeField]
    private bool unlockCursorForTurnUi = true;

    private readonly List<Team> runtimeTeams = new List<Team>();
    private readonly List<Skeleton> runtimePlayers = new List<Skeleton>();
    private readonly Dictionary<Skeleton, string> playerNames = new Dictionary<Skeleton, string>();
    private CardGameModel? cardGame;
    private CardGameModel? subscribedGame;
    private Coroutine? continueFlowCoroutine;
    private bool cursorStateCaptured;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;

    public CardGameModel? CardGame => cardGame;
    public IReadOnlyList<Team> Teams => runtimeTeams;
    public IReadOnlyList<Skeleton> Players => runtimePlayers;
    public Skeleton? CurrentPlayer => IsValidPlayerIndex(currentPlayerIndex) ? runtimePlayers[currentPlayerIndex] : null;
    public Skeleton? ActiveTurnPlayer => cardGame?.round?.CurrentPlayer;

    public event Action<CardGameModel>? OnGameStarted;
    public event Action<Skeleton>? OnTurnStarted;
    public event Action<Skeleton>? OnTurnEnded;
    public event Action<GamePhase>? OnPhaseChanged;

    private void Awake()
    {
        EnsureTurnUI();
        CloseTurnUI();
    }

    private void Start()
    {
        if (enableOnStart)
            StartGame();
    }

    private void OnDestroy()
    {
        UnsubscribeFromGame();
    }

    public void StartGame()
    {
        UnsubscribeFromGame();
        BuildRuntimeState();

        if (runtimePlayers.Count == 0)
        {
            Debug.LogWarning("Cannot start card game without players.", this);
            return;
        }

        cardGame = new CardGameModel(runtimeTeams, runtimePlayers);
        SubscribeToGame(cardGame);
        OnGameStarted?.Invoke(cardGame);

        cardGame.DealPlayersCards();
        cardGame.ShowCombinations();
        cardGame.StartRound();
        cardGame.StartBettingRound();
    }

    public bool TryCallCurrentPlayer(DeclaredCombinationTier tier)
    {
        return TryPerformTurnAction(ActiveTurnPlayer, CardGameTurnAction.Call, tier, 0);
    }

    public bool TryRaiseCurrentPlayer(DeclaredCombinationTier tier, int targetStake)
    {
        return TryPerformTurnAction(ActiveTurnPlayer, CardGameTurnAction.Raise, tier, targetStake);
    }

    public bool TryAllInCurrentPlayer(DeclaredCombinationTier tier)
    {
        return TryPerformTurnAction(ActiveTurnPlayer, CardGameTurnAction.AllIn, tier, 0);
    }

    public bool TryTakeCardCurrentPlayer()
    {
        return TryPerformTurnAction(ActiveTurnPlayer, CardGameTurnAction.TakeCard, DeclaredCombinationTier.Easy, 0);
    }

    public bool TryFoldCurrentPlayer()
    {
        return TryPerformTurnAction(ActiveTurnPlayer, CardGameTurnAction.Fold, DeclaredCombinationTier.Easy, 0);
    }

    public bool TryEndCurrentTurn()
    {
        return TryPerformTurnAction(ActiveTurnPlayer, CardGameTurnAction.EndTurn, DeclaredCombinationTier.Easy, 0);
    }

    public bool TryPerformTurnAction(
        Skeleton? player,
        CardGameTurnAction action,
        DeclaredCombinationTier tier,
        int targetStake)
    {
        if (player == null || cardGame?.round == null)
            return false;

        try
        {
            CardGameRound round = cardGame.round;
            switch (action)
            {
                case CardGameTurnAction.Call:
                    return TryCall(round, player, tier);
                case CardGameTurnAction.Raise:
                    return TryRaise(round, player, tier, targetStake);
                case CardGameTurnAction.AllIn:
                    return TryAllIn(round, player, tier);
                case CardGameTurnAction.TakeCard:
                    return TryTakeCard(round, player);
                case CardGameTurnAction.Fold:
                    return TryFold(round, player);
                case CardGameTurnAction.EndTurn:
                    return TryEndTurn(round, player);
                default:
                    return false;
            }
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogWarning(exception.Message, this);
            return false;
        }
    }

    public List<CardGameTurnAction> GetValidTurnActions(Skeleton? player)
    {
        var actions = new List<CardGameTurnAction>();
        if (player == null || cardGame?.round == null || cardGame.phase != GamePhase.Betting)
            return actions;

        CardGameRound round = cardGame.round;
        if (round.CurrentPlayer != player)
            return actions;

        DeclaredCombinationTier tier = GetLowestAllowedTier(round, player);
        if (CanCall(round, player, tier))
            actions.Add(CardGameTurnAction.Call);
        if (GetSuggestedRaiseStake(player) > round.currentParticipationPrice)
            actions.Add(CardGameTurnAction.Raise);
        if (round.CanAllIn(player, tier))
            actions.Add(CardGameTurnAction.AllIn);
        if (round.CanTakeCard(player))
            actions.Add(CardGameTurnAction.TakeCard);
        if (CanFold(round, player))
            actions.Add(CardGameTurnAction.Fold);
        if (round.HasMatchedBet(player))
            actions.Add(CardGameTurnAction.EndTurn);

        return actions;
    }

    public int GetCurrentParticipationPrice()
    {
        return cardGame?.round?.currentParticipationPrice ?? 0;
    }

    public int GetSuggestedRaiseStake(Skeleton? player)
    {
        if (player == null || cardGame?.round == null || !cardGame.round.CanRaise(player))
            return 0;

        DeclaredCombinationTier tier = GetLowestAllowedTier(cardGame.round, player);
        List<StakeAsset> assets = SelectAssetsForStake(player, cardGame.round.currentParticipationPrice + 1);
        int stakeValue = CalculateStakeValue(assets);
        return stakeValue > cardGame.round.currentParticipationPrice && cardGame.round.CanRaise(player, assets, tier)
            ? stakeValue
            : 0;
    }

    public string GetPlayerDisplayName(Skeleton? player)
    {
        if (player == null)
            return "None";

        if (playerNames.TryGetValue(player, out string name) && !string.IsNullOrWhiteSpace(name))
            return name;

        int index = runtimePlayers.IndexOf(player);
        return index >= 0 ? $"Player {index + 1}" : "Unknown";
    }

    public string BuildTurnSummary(Skeleton? player)
    {
        if (cardGame == null)
            return "Game is not started.";

        CardGameRound? round = cardGame.round;
        if (round == null || player == null)
            return $"Phase: {cardGame.phase}";

        PlayerBetState state = round.playerStates[player];
        string hand = string.Join(", ", player.Hand.GetCards().Select(FormatCard));
        string table = string.Join(", ", round.tableCards.Select(FormatCard));
        string actions = string.Join(", ", GetValidTurnActions(player));

        return
            $"Phase: {cardGame.phase}\n" +
            $"Turn: {GetPlayerDisplayName(player)}\n" +
            $"Current price: {round.currentParticipationPrice}\n" +
            $"Committed: {state.committedValue}\n" +
            $"Declared: {(state.declaredTarget.HasValue ? state.declaredTarget.Value.ToString() : "None")}\n" +
            $"Hand: {hand}\n" +
            $"Table: {table}\n" +
            $"Valid actions: {actions}";
    }

    public void RefreshTurnUI()
    {
        if (turnUi != null)
            turnUi.Refresh();
    }

    private void BuildRuntimeState()
    {
        runtimeTeams.Clear();
        runtimePlayers.Clear();
        playerNames.Clear();
        CloseTurnUI();

        if (createTestSetupWhenEmpty && (teamSetups.Count == 0 || playerSetups.Count == 0))
        {
            BuildTestRuntimeState();
            return;
        }

        foreach (TeamSetup setup in teamSetups)
        {
            var team = new Team();
            runtimeTeams.Add(team);

            foreach (StakeAsset asset in setup.assets)
            {
                if (asset != null)
                    team.RegisterAsset(asset);
            }
        }

        foreach (PlayerSetup setup in playerSetups)
        {
            if (!IsValidTeamIndex(setup.teamIndex))
            {
                Debug.LogWarning($"Player '{setup.playerName}' has invalid team index {setup.teamIndex}.", this);
                continue;
            }

            Team team = runtimeTeams[setup.teamIndex];
            var player = new Skeleton(team);
            team.AddSkeleton(player);
            runtimePlayers.Add(player);
            playerNames[player] = setup.playerName;
        }

        currentPlayerIndex = Mathf.Clamp(currentPlayerIndex, 0, Math.Max(0, runtimePlayers.Count - 1));
    }

    private void BuildTestRuntimeState()
    {
        Team teamA = CreateTestTeam();
        Team teamB = CreateTestTeam();

        runtimeTeams.Add(teamA);
        runtimeTeams.Add(teamB);

        CreateTestPlayer("Current Player", teamA);
        CreateTestPlayer("AI Player A", teamB);
        CreateTestPlayer("AI Player B", teamA);
        CreateTestPlayer("AI Player C", teamB);

        currentPlayerIndex = 0;
    }

    private Team CreateTestTeam()
    {
        var team = new Team();
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.Soul, 1));
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.Soul, 2));
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.OtherTeamAsset, 3));
        return team;
    }

    private void CreateTestPlayer(string playerName, Team team)
    {
        var player = new Skeleton(team);
        team.AddSkeleton(player);
        runtimePlayers.Add(player);
        playerNames[player] = playerName;
    }

    private bool TryCall(CardGameRound round, Skeleton player, DeclaredCombinationTier tier)
    {
        tier = NormalizeTier(round, player, tier);
        if (!CanCall(round, player, tier))
            return false;

        round.Call(player, SelectAssetsForStake(player, round.currentParticipationPrice), tier);
        RefreshTurnUI();
        return true;
    }

    private bool TryRaise(CardGameRound round, Skeleton player, DeclaredCombinationTier tier, int targetStake)
    {
        tier = NormalizeTier(round, player, tier);
        if (targetStake <= round.currentParticipationPrice)
            targetStake = GetSuggestedRaiseStake(player);

        List<StakeAsset> assets = SelectAssetsForStake(player, targetStake);
        if (assets.Count == 0 || !round.CanRaise(player, assets, tier))
            return false;

        round.Raise(player, assets, tier);
        RefreshTurnUI();
        return true;
    }

    private bool TryAllIn(CardGameRound round, Skeleton player, DeclaredCombinationTier tier)
    {
        tier = NormalizeTier(round, player, tier);
        if (!round.CanAllIn(player, tier))
            return false;

        round.AllIn(player, tier);
        RefreshTurnUI();
        return true;
    }

    private bool TryTakeCard(CardGameRound round, Skeleton player)
    {
        if (!round.CanTakeCard(player))
            return false;

        round.TakeCard(player);
        RefreshTurnUI();
        return true;
    }

    private bool TryFold(CardGameRound round, Skeleton player)
    {
        if (!CanFold(round, player))
            return false;

        round.Fold(player);
        RefreshTurnUI();
        return true;
    }

    private bool TryEndTurn(CardGameRound round, Skeleton player)
    {
        if (!round.HasMatchedBet(player))
            return false;

        round.EndTurn(player);
        RefreshTurnUI();
        return true;
    }

    private bool CanCall(CardGameRound round, Skeleton player, DeclaredCombinationTier tier)
    {
        return round.CanCall(player, SelectAssetsForStake(player, round.currentParticipationPrice), tier);
    }

    private static bool CanFold(CardGameRound round, Skeleton player)
    {
        return round.CurrentPlayer == player && round.ActivePlayers.Count > 1;
    }

    private static List<StakeAsset> SelectAssetsForStake(Skeleton player, int targetStake)
    {
        var selectedAssets = new List<StakeAsset>();
        int selectedValue = 0;

        foreach (StakeAsset asset in player.team.Assets.Where(asset => asset != null).OrderBy(asset => asset.stakeValue))
        {
            selectedAssets.Add(asset);
            selectedValue += Math.Max(0, asset.stakeValue);

            if (selectedValue >= targetStake)
                break;
        }

        return selectedAssets;
    }

    private static DeclaredCombinationTier NormalizeTier(
        CardGameRound round,
        Skeleton player,
        DeclaredCombinationTier tier)
    {
        DeclaredCombinationTier lowestAllowedTier = GetLowestAllowedTier(round, player);
        return tier < lowestAllowedTier ? lowestAllowedTier : tier;
    }

    private static DeclaredCombinationTier GetLowestAllowedTier(CardGameRound round, Skeleton player)
    {
        PlayerBetState state = round.playerStates[player];
        return state.declaredTarget ?? DeclaredCombinationTier.Easy;
    }

    private static int CalculateStakeValue(IList<StakeAsset> assets)
    {
        int value = 0;
        foreach (StakeAsset asset in assets)
        {
            if (asset != null)
                value += Math.Max(0, asset.stakeValue);
        }

        return value;
    }

    private void SubscribeToGame(CardGameModel game)
    {
        subscribedGame = game;
        subscribedGame.OnTurnStarted += HandleTurnStarted;
        subscribedGame.OnTurnEnded += HandleTurnEnded;
        subscribedGame.OnPhaseChanged += HandlePhaseChanged;
        subscribedGame.OnRoundEnded += HandleRoundEnded;
    }

    private void UnsubscribeFromGame()
    {
        if (subscribedGame == null)
            return;

        subscribedGame.OnTurnStarted -= HandleTurnStarted;
        subscribedGame.OnTurnEnded -= HandleTurnEnded;
        subscribedGame.OnPhaseChanged -= HandlePhaseChanged;
        subscribedGame.OnRoundEnded -= HandleRoundEnded;
        subscribedGame = null;
    }

    private void HandleTurnStarted(Skeleton player)
    {
        OnTurnStarted?.Invoke(player);

        if (player == CurrentPlayer)
            OpenTurnUI(player);
        else
            CloseTurnUI();
    }

    private void HandleTurnEnded(Skeleton player)
    {
        OnTurnEnded?.Invoke(player);

        if (player == CurrentPlayer)
            CloseTurnUI();
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        OnPhaseChanged?.Invoke(phase);

        if (phase == GamePhase.AddingCards || phase == GamePhase.End)
            ScheduleFlowContinuation();
    }

    private void HandleRoundEnded(RoundResult result)
    {
        CloseTurnUI();
    }

    private void ScheduleFlowContinuation()
    {
        if (continueFlowCoroutine != null)
            StopCoroutine(continueFlowCoroutine);

        continueFlowCoroutine = StartCoroutine(ContinueFlowAfterPhaseChange());
    }

    private IEnumerator ContinueFlowAfterPhaseChange()
    {
        yield return null;

        continueFlowCoroutine = null;
        if (cardGame?.round == null)
            yield break;

        if (cardGame.phase == GamePhase.AddingCards)
        {
            cardGame.DealTableCards();
            cardGame.StartBettingRound();
        }
        else if (cardGame.phase == GamePhase.End)
        {
            cardGame.round.DetermineWinners();
            cardGame.round.ResolvePot();
        }
    }

    private void OpenTurnUI(Skeleton player)
    {
        EnsureTurnUI();

        GameObject? target = turnUiObject != null ? turnUiObject : turnUi != null ? turnUi.gameObject : null;
        if (target != null)
            target.SetActive(true);

        UnlockCursorForUI();

        if (turnUi != null)
            turnUi.Show(this, player);
    }

    private void CloseTurnUI()
    {
        GameObject? target = turnUiObject != null ? turnUiObject : turnUi != null ? turnUi.gameObject : null;
        if (target != null)
            target.SetActive(false);

        RestoreCursorAfterUI();
    }

    private void EnsureTurnUI()
    {
        if (turnUi == null && turnUiObject != null)
            turnUi = turnUiObject.GetComponent<TurnUI>();

        if (turnUi == null)
            turnUi = FindFirstObjectByType<TurnUI>(FindObjectsInactive.Include);

        if (turnUi == null && createTestTurnUiWhenMissing)
        {
            var uiGameObject = new GameObject("Test Turn UI");
            turnUi = uiGameObject.AddComponent<TurnUI>();
        }

        if (turnUiObject == null && turnUi != null)
            turnUiObject = turnUi.gameObject;
    }

    private void UnlockCursorForUI()
    {
        if (!unlockCursorForTurnUi)
            return;

        if (!cursorStateCaptured)
        {
            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            cursorStateCaptured = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestoreCursorAfterUI()
    {
        if (!unlockCursorForTurnUi || !cursorStateCaptured)
            return;

        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisible;
        cursorStateCaptured = false;
    }

    private bool IsValidTeamIndex(int index)
    {
        return index >= 0 && index < runtimeTeams.Count;
    }

    private bool IsValidPlayerIndex(int index)
    {
        return index >= 0 && index < runtimePlayers.Count;
    }

    private static string FormatCard(CardData card)
    {
        return card == null ? "None" : $"{card.Value} of {card.Suit}";
    }
}
