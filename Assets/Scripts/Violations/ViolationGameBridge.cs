#nullable enable
using System;
using UnityEngine;
using static CardGame;

[DisallowMultipleComponent]
[RequireComponent(typeof(GameManager))]
public sealed class ViolationGameBridge : MonoBehaviour {
    private GameManager? gameManager;
    private CardGame? subscribedGame;
    private ViolationGameMoment currentMoment = ViolationGameMoment.OutOfRound;
    
// ISSUE 20 DEBUG
#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool enableViolationDebugHotkeys = true;
#endif

    public ViolationsHandler Service { get; private set; } = new ViolationsHandler();
    public ViolationGameMoment CurrentMoment => currentMoment;

    private void Awake() {
        gameManager = GetComponent<GameManager>();
        Service = new ViolationsHandler(gameManager != null ? gameManager.Teams : null);
    }

    private void OnEnable() {
        if (gameManager == null)
            gameManager = GetComponent<GameManager>();

        if (gameManager != null) {
            gameManager.OnGameCreated += HandleGameCreated;
            if (gameManager.CardGame != null)
                HandleGameCreated(gameManager.CardGame);
        }
    }

    private void OnDisable() {
        if (gameManager != null)
            gameManager.OnGameCreated -= HandleGameCreated;

        UnsubscribeGame();
    }
    
// ISSUE 20 DEBUG
#if UNITY_EDITOR
    private void Update() {
        if (!enableViolationDebugHotkeys) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F6)) {
            DebugLogViolationState();
        }
        
        if (Input.GetKeyDown(KeyCode.F7)) {
            DebugSimulateCardPeekingComplaint();
        }
        
        if (Input.GetKeyDown(KeyCode.F8)) {
            DebugSimulateTheftComplaint();
        }
        
        if (Input.GetKeyDown(KeyCode.F9)) {
            DebugSimulateAllSidesCheatedCompensation();
        }
    }
#endif

    public ViolationComplaintResult SubmitComplaint(Team reportingTeam, Skeleton accusedPlayer, ViolationType type) {
        return Service.SubmitComplaint(reportingTeam, accusedPlayer, type, DateTime.UtcNow);
    }

    public bool RecordCardPeeking(CardVisibilityContext context) {
        if (ViolationDetector.TryCreateCardPeekingCandidate(
                context,
                currentMoment,
                DateTime.UtcNow,
                GetActorKind(context.Viewer),
                out ViolationCandidate? candidate)
            && candidate != null) {
            Service.RecordCandidate(candidate);
            return true;
        }

        return false;
    }

    public bool RecordTheft(TheftContext context) {
        if (ViolationDetector.TryCreateTheftCandidate(
                context,
                currentMoment,
                DateTime.UtcNow,
                GetActorKind(context.Actor),
                out ViolationCandidate? candidate)
            && candidate != null) {
            Service.RecordCandidate(candidate);
            return true;
        }

        return false;
    }

    public ViolationCandidate RecordPlayerLeftSeat(Skeleton offender, bool isForbiddenByCurrentPhase) {
        ViolationCandidate candidate = ViolationDetector.CreateLeavingSeatCandidate(
            offender,
            currentMoment,
            DateTime.UtcNow,
            GetActorKind(offender),
            isForbiddenByCurrentPhase);
        Service.RecordCandidate(candidate);
        return candidate;
    }

    public ViolationCandidate RecordMissedTimedTurn(Skeleton offender, bool turnTimerExpiredForCurrentPlayer) {
        ViolationCandidate candidate = ViolationDetector.CreateMissedTimedTurnCandidate(
            offender,
            currentMoment,
            DateTime.UtcNow,
            GetActorKind(offender),
            turnTimerExpiredForCurrentPlayer);
        Service.RecordCandidate(candidate);
        return candidate;
    }

    private void HandleGameCreated(CardGame game) {
        UnsubscribeGame();
        subscribedGame = game;
        Service.RegisterTeams(gameManager != null ? gameManager.Teams : Array.Empty<Team>());

        game.OnPhaseChanged += HandlePhaseChanged;
        game.OnBettingRoundStarted += HandleBettingRoundStarted;
        game.OnBettingRoundEnded += HandleBettingRoundEnded;
        game.OnTurnStarted += HandleTurnStarted;
        game.OnTurnEnded += HandleTurnEnded;
        game.OnRoundEnded += HandleRoundEnded;

        RefreshMomentFromGame(game);
    }

    private void UnsubscribeGame() {
        if (subscribedGame == null)
            return;

        subscribedGame.OnPhaseChanged -= HandlePhaseChanged;
        subscribedGame.OnBettingRoundStarted -= HandleBettingRoundStarted;
        subscribedGame.OnBettingRoundEnded -= HandleBettingRoundEnded;
        subscribedGame.OnTurnStarted -= HandleTurnStarted;
        subscribedGame.OnTurnEnded -= HandleTurnEnded;
        subscribedGame.OnRoundEnded -= HandleRoundEnded;
        subscribedGame = null;
    }

    private void HandlePhaseChanged(GamePhase phase) {
        RefreshMomentFromGame(subscribedGame);
    }

    private void HandleBettingRoundStarted(Round round) {
        currentMoment = CreateMoment(round, ViolationMatchPhase.ActiveTurn, round.CurrentPlayer, "Betting");
    }

    private void HandleBettingRoundEnded(Round round) {
        currentMoment = CreateMoment(round, ViolationMatchPhase.ActiveRound, null, "BettingRoundEnded");
    }

    private void HandleTurnStarted(Skeleton player) {
        Round? round = subscribedGame?.round;
        currentMoment = round != null
            ? CreateMoment(round, ViolationMatchPhase.ActiveTurn, player, subscribedGame!.phase.ToString())
            : new ViolationGameMoment(ViolationMatchPhase.ActiveTurn, true, player);
    }

    private void HandleTurnEnded(Skeleton player) {
        RefreshMomentFromGame(subscribedGame);
    }

    private void HandleRoundEnded(RoundResult result) {
        currentMoment = new ViolationGameMoment(ViolationMatchPhase.EndRound, false, externalPhaseName: "End");
        Service.PruneExpired(DateTime.UtcNow);
    }

    private void RefreshMomentFromGame(CardGame? game) {
        if (game == null) {
            currentMoment = ViolationGameMoment.OutOfRound;
            return;
        }

        Round? round = game.round;
        ViolationMatchPhase phase = MapPhase(game.phase);
        Skeleton? currentPlayer = phase == ViolationMatchPhase.ActiveTurn && round != null ? round.CurrentPlayer : null;
        currentMoment = round != null
            ? CreateMoment(round, phase, currentPlayer, game.phase.ToString())
            : new ViolationGameMoment(phase, phase != ViolationMatchPhase.OutOfRound, currentPlayer, -1,
                game.phase.ToString());
    }

    private ViolationGameMoment CreateMoment(Round round, ViolationMatchPhase phase, Skeleton? currentTurnPlayer,
        string externalPhaseName) {
        return new ViolationGameMoment(
            phase,
            phase != ViolationMatchPhase.OutOfRound && phase != ViolationMatchPhase.EndRound,
            currentTurnPlayer,
            round.BettingRound,
            externalPhaseName);
    }

// ISSUE 20 DEBUG
#if UNITY_EDITOR
    [ContextMenu("Debug/Violations: Log state")]
    private void DebugLogViolationState() {
        if (!TryGetDebugActors(out Team? reportingTeam, out Team? accusedTeam,
                out Skeleton? reportingPlayer, out Skeleton? accusedPlayer)) {
            return;
        }

        Debug.Log(
            $"[Violations Debug] Moment={currentMoment.Phase}, RoundActive={currentMoment.IsRoundActive}, " +
            $"ReportingTeamWarnings={Service.WarningLog.GetWarnings(reportingTeam!)}, " +
            $"AccusedTeamWarnings={Service.WarningLog.GetWarnings(accusedTeam!)}, " +
            $"Candidates={Service.CandidateHistory.Count}, Confirmed={Service.ConfirmedHistory.Count}, " +
            $"ReportingPlayer={(reportingPlayer != null ? reportingPlayer.GetHashCode().ToString() : "none")}, " +
            $"AccusedPlayer={(accusedPlayer != null ? accusedPlayer.GetHashCode().ToString() : "none")}",
            this);
    }

    [ContextMenu("Debug/Violations: Simulate card peeking complaint")]
    private void DebugSimulateCardPeekingComplaint() {
        if (!TryGetDebugActors(out Team? reportingTeam, out Team? accusedTeam, out _, out Skeleton? accusedPlayer))
            return;

        var context = new CardVisibilityContext(
            viewer: accusedPlayer!,
            cardOwnerTeam: reportingTeam!,
            physicalZone: CardWorldZone.OpponentHand,
            relation: CardVisibilityRelation.OpponentCard,
            isPhysicallyPresent: true,
            isTemporarilyVisible: false,
            hasExplicitVisibilityRight: false);

        bool recorded = RecordCardPeeking(context);
        ViolationComplaintResult result = SubmitComplaint(reportingTeam!, accusedPlayer!, ViolationType.CardPeeking);
        Debug.Log(
            $"[Violations Debug] CardPeeking recorded={recorded}, complaint={result.Status}, " +
            $"accusedTeamWarnings={Service.WarningLog.GetWarnings(accusedTeam!)}",
            this);
    }

    [ContextMenu("Debug/Violations: Simulate theft complaint")]
    private void DebugSimulateTheftComplaint() {
        if (!TryGetDebugActors(out Team? reportingTeam, out Team? accusedTeam, out _, out Skeleton? accusedPlayer))
            return;

        var context = new TheftContext(
            actor: accusedPlayer!,
            assetOwnerTeam: reportingTeam!,
            eventKind: TheftEventKind.PickedUpOpponentCards,
            isAllowedByOwnershipRules: false);

        bool recorded = RecordTheft(context);
        ViolationComplaintResult result = SubmitComplaint(reportingTeam!, accusedPlayer!, ViolationType.Theft);
        Debug.Log(
            $"[Violations Debug] Theft recorded={recorded}, complaint={result.Status}, " +
            $"accusedTeamWarnings={Service.WarningLog.GetWarnings(accusedTeam!)}",
            this);
    }

    [ContextMenu("Debug/Violations: Simulate all-sides compensation")]
    private void DebugSimulateAllSidesCheatedCompensation() {
        if (!TryGetDebugActors(out Team? teamA, out Team? teamB, out Skeleton? playerA, out Skeleton? playerB))
            return;

        RecordMissedTimedTurn(playerB!, true);
        ViolationComplaintResult first = SubmitComplaint(teamA!, playerB!, ViolationType.MissedTimedTurn);
        RecordMissedTimedTurn(playerA!, true);
        ViolationComplaintResult second = SubmitComplaint(teamB!, playerA!, ViolationType.MissedTimedTurn);

        Debug.Log(
            $"[Violations Debug] Compensation complaints={first.Status}/{second.Status}, " +
            $"teamAWarnings={Service.WarningLog.GetWarnings(teamA!)}, " +
            $"teamBWarnings={Service.WarningLog.GetWarnings(teamB!)}",
            this);
    }

    private bool TryGetDebugActors(out Team? reportingTeam, out Team? accusedTeam, out Skeleton? reportingPlayer,
        out Skeleton? accusedPlayer) {
        reportingTeam = null;
        accusedTeam = null;
        reportingPlayer = null;
        accusedPlayer = null;

        if (gameManager == null)
            gameManager = GetComponent<GameManager>();
        if (gameManager == null || gameManager.Teams.Count < 2 || gameManager.Players.Count < 2) {
            Debug.LogWarning("[Violations Debug] Need a started GameManager with at least two teams and two players.", this);
            return false;
        }

        reportingTeam = gameManager.Teams[0];
        accusedTeam = gameManager.Teams[1];
        foreach (Skeleton player in gameManager.Players) {
            if (reportingPlayer == null && player.team == reportingTeam)
                reportingPlayer = player;
            if (accusedPlayer == null && player.team == accusedTeam)
                accusedPlayer = player;
        }

        if (reportingPlayer != null && accusedPlayer != null)
            return true;

        Debug.LogWarning("[Violations Debug] Could not find one player for each of the first two teams.", this);
        return false;
    }
#endif
    private ViolationMatchPhase MapPhase(GamePhase phase) {
        switch (phase) {
            case GamePhase.BettingRoundStart:
                return ViolationMatchPhase.Discussion;
            case GamePhase.Betting:
                return ViolationMatchPhase.ActiveTurn;
            case GamePhase.ShowingCombinations:
            case GamePhase.RoundStart:
            case GamePhase.AddingCards:
                return ViolationMatchPhase.ActiveRound;
            case GamePhase.End:
                return ViolationMatchPhase.EndRound;
            default:
                return ViolationMatchPhase.OutOfRound;
        }
    }

    private ViolationActorKind GetActorKind(Skeleton player) {
        if (gameManager == null || player == null)
            return ViolationActorKind.Unknown;

        if (ReferenceEquals(gameManager.LocalPlayer, player))
            return ViolationActorKind.Human;
        foreach (Skeleton knownPlayer in gameManager.Players) {
            if (ReferenceEquals(knownPlayer, player))
                return ViolationActorKind.AI;
        }

        return ViolationActorKind.Unknown;
    }
}