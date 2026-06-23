#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CardGameModel = CardGame;
using CardGameRound = CardGame.Round;

[RequireComponent(typeof(GameManager))]
public sealed class TestConsoleLogger : MonoBehaviour
{
    private GameManager? gameManager;
    private CardGameModel? subscribedGame;
    private readonly List<Skeleton> subscribedTurnPlayers = new List<Skeleton>();

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    private void OnEnable()
    {
        gameManager = gameManager != null ? gameManager : GetComponent<GameManager>();
        if (gameManager == null)
            return;

        gameManager.OnGameCreated += HandleGameCreated;
        Attach(gameManager.CardGame);
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnGameCreated -= HandleGameCreated;

        Detach();
    }

    private void HandleGameCreated(CardGameModel game)
    {
        Attach(game);
        LogTeamOwnership("Initial team ownership");
    }

    private void Attach(CardGameModel? game)
    {
        if (subscribedGame == game)
            return;

        Detach();
        subscribedGame = game;

        if (subscribedGame == null)
            return;

        subscribedGame.OnPhaseChanged += HandlePhaseChanged;
        subscribedGame.OnRoundStarted += HandleRoundStarted;
        subscribedGame.OnBettingRoundStarted += HandleBettingRoundStarted;
        subscribedGame.OnBettingRoundEnded += HandleBettingRoundEnded;
        subscribedGame.OnTargetDeclared += HandleTargetDeclared;
        subscribedGame.OnTargetUpgraded += HandleTargetUpgraded;
        subscribedGame.OnPriceMatched += HandlePriceMatched;
        subscribedGame.OnPriceRaised += HandlePriceRaised;
        subscribedGame.OnPlayerFolded += HandlePlayerFolded;
        subscribedGame.OnCardTaken += HandleCardTaken;
        subscribedGame.OnTableCardsDealt += HandleTableCardsDealt;
        subscribedGame.OnPotResolved += HandlePotResolved;
        subscribedGame.OnRoundEnded += HandleRoundEnded;

        if (gameManager == null)
            return;

        foreach (Skeleton player in gameManager.Players)
        {
            if (subscribedGame.TurnStartedByPlayer.TryGetValue(player, out CardGameModel.PlayerTurnEvent turnStarted))
                turnStarted.Fired += HandleTurnStarted;
            if (subscribedGame.TurnEndedByPlayer.TryGetValue(player, out CardGameModel.PlayerTurnEvent turnEnded))
                turnEnded.Fired += HandleTurnEnded;
            subscribedTurnPlayers.Add(player);
        }
    }

    private void Detach()
    {
        if (subscribedGame == null)
            return;

        subscribedGame.OnPhaseChanged -= HandlePhaseChanged;
        subscribedGame.OnRoundStarted -= HandleRoundStarted;
        subscribedGame.OnBettingRoundStarted -= HandleBettingRoundStarted;
        subscribedGame.OnBettingRoundEnded -= HandleBettingRoundEnded;
        foreach (Skeleton player in subscribedTurnPlayers)
        {
            if (subscribedGame.TurnStartedByPlayer.TryGetValue(player, out CardGameModel.PlayerTurnEvent turnStarted))
                turnStarted.Fired -= HandleTurnStarted;
            if (subscribedGame.TurnEndedByPlayer.TryGetValue(player, out CardGameModel.PlayerTurnEvent turnEnded))
                turnEnded.Fired -= HandleTurnEnded;
        }
        subscribedGame.OnTargetDeclared -= HandleTargetDeclared;
        subscribedGame.OnTargetUpgraded -= HandleTargetUpgraded;
        subscribedGame.OnPriceMatched -= HandlePriceMatched;
        subscribedGame.OnPriceRaised -= HandlePriceRaised;
        subscribedGame.OnPlayerFolded -= HandlePlayerFolded;
        subscribedGame.OnCardTaken -= HandleCardTaken;
        subscribedGame.OnTableCardsDealt -= HandleTableCardsDealt;
        subscribedGame.OnPotResolved -= HandlePotResolved;
        subscribedGame.OnRoundEnded -= HandleRoundEnded;
        subscribedTurnPlayers.Clear();
        subscribedGame = null;
    }

    private void HandlePhaseChanged(CardGameModel.GamePhase phase)
    {
        Debug.Log($"[Test] Phase: {phase}", this);

        if (phase == CardGameModel.GamePhase.ShowingCombinations)
            LogNonHumanHands("Debug non-human hands after private deal");
    }

    private void HandleRoundStarted(CardGameRound round)
    {
        Debug.Log($"[Test] Round started. Table: {FormatCards(round.TableCards)}", this);
        Debug.Log($"[Test] Round combinations: {FormatCombinationSet(round.Combinations)}", this);
        LogTeamOwnership("Team ownership at round start");
    }

    private void HandleBettingRoundStarted(CardGameRound round)
    {
        Debug.Log($"[Test] Betting started. Current price: {round.currentParticipationPrice}. Table: {FormatCards(round.TableCards)}", this);
    }

    private void HandleBettingRoundEnded(CardGameRound round)
    {
        Debug.Log($"[Test] Betting ended. Betting round: {round.BettingRound + 1}. Table: {FormatCards(round.TableCards)}", this);
    }

    private void HandleTurnStarted(Skeleton player)
    {
        Debug.Log($"[Test] Turn started: {PlayerLabel(player)} ({ControlLabel(player)}).", this);
    }

    private void HandleTurnEnded(Skeleton player)
    {
        Debug.Log($"[Test] Turn ended: {PlayerLabel(player)}.", this);
    }

    private void HandleTargetDeclared(Skeleton player, DeclaredCombinationTier tier)
    {
        Debug.Log($"[Test] {PlayerLabel(player)} declared target {tier}.", this);
    }

    private void HandleTargetUpgraded(Skeleton player, DeclaredCombinationTier tier)
    {
        Debug.Log($"[Test] {PlayerLabel(player)} upgraded target to {tier}.", this);
    }

    private void HandlePriceMatched(Skeleton player, int price)
    {
        Debug.Log($"[Test] {PlayerLabel(player)} matched price {price}.", this);
    }

    private void HandlePriceRaised(Skeleton player, int price)
    {
        Debug.Log($"[Test] {PlayerLabel(player)} raised price to {price}.", this);
    }

    private void HandlePlayerFolded(Skeleton player)
    {
        Debug.Log($"[Test] {PlayerLabel(player)} folded.", this);
    }

    private void HandleCardTaken(Skeleton player, CardData card)
    {
        string cardText = gameManager != null && IsAiControlled(player) ? $" Card: {FormatCard(card)}." : string.Empty;
        Debug.Log($"[Test] {PlayerLabel(player)} took a card.{cardText}", this);
        LogNonHumanHands("Debug non-human hands after card draw");
    }

    private void HandleTableCardsDealt(IReadOnlyList<CardData> cards)
    {
        Debug.Log($"[Test] Table cards dealt: {FormatCards(cards)}. Full table: {FormatCards(subscribedGame?.round?.TableCards)}", this);
    }

    private void HandlePotResolved(IReadOnlyList<Team> winners, IReadOnlyList<StakeAsset> assets)
    {
        Debug.Log($"[Test] Pot resolved. Winners: {FormatTeams(winners)}. Assets: {FormatAssets(assets)}", this);
        LogTeamOwnership("Team ownership after pot resolution");
    }

    private void HandleRoundEnded(RoundResult result)
    {
        Debug.Log($"[Test] Round ended. Winners: {FormatTeams(result.winners)}. Scores: {FormatScores(result.scores)}. Distribution: {FormatDistribution(result.assetDistribution)}", this);
    }

    private void LogNonHumanHands(string title)
    {
        if (gameManager == null)
            return;

        IEnumerable<string> hands = gameManager.Players
            .Where(IsAiControlled)
            .Select(player => $"{PlayerLabel(player)}: {FormatCards(player.Hand.GetCards())}");

        Debug.Log($"[Test] {title}: {JoinOrNone(hands)}", this);
    }

    private void LogTeamOwnership(string title)
    {
        if (gameManager == null)
            return;

        IEnumerable<string> ownership = gameManager.Teams
            .Select(team => $"{TeamLabel(team)} owns {FormatAssets(team.Assets)}");

        Debug.Log($"[Test] {title}: {JoinOrNone(ownership)}", this);
    }

    private string PlayerLabel(Skeleton player)
    {
        if (gameManager == null)
            return "Player ?";

        int playerIndex = IndexOf(gameManager.Players, player);
        return playerIndex >= 0 ? $"Player {playerIndex + 1} / {TeamLabel(player.team)}" : "Player ?";
    }

    private string TeamLabel(Team team)
    {
        if (gameManager == null)
            return "Team ?";

        int teamIndex = IndexOf(gameManager.Teams, team);
        return teamIndex >= 0 ? $"Team {teamIndex + 1}" : "Team ?";
    }

    private string ControlLabel(Skeleton player)
    {
        return gameManager != null && ReferenceEquals(gameManager.LocalPlayer, player) ? "human UI" : "AI";
    }

    private bool IsAiControlled(Skeleton player)
    {
        return gameManager != null && !ReferenceEquals(gameManager.LocalPlayer, player);
    }

    private string FormatTeams(IReadOnlyList<Team>? teams)
    {
        return teams == null || teams.Count == 0 ? "none" : string.Join(", ", teams.Select(TeamLabel));
    }

    private static string FormatCards(IReadOnlyList<CardData>? cards)
    {
        return cards == null || cards.Count == 0 ? "none" : string.Join(", ", cards.Select(FormatCard));
    }

    private static string FormatCard(CardData card)
    {
        return card == null ? "null" : $"{card.Value} of {card.Suit}";
    }

    private static string FormatCombinationSet(RoundCombinationSet combinations)
    {
        return combinations == null
            ? "none"
            : string.Join("; ", combinations.GetAll().Select(entry => $"{entry.difficulty}: {FormatCombination(entry.combination)}"));
    }

    private static string FormatCombination(Combination combination)
    {
        return combination == null
            ? "none"
            : string.Join(" + ", combination.Rules.Select(FormatCombinationRule));
    }

    private static string FormatCombinationRule(CombinationRule rule)
    {
        if (rule == null)
            return "null";

        string value = rule.ParamValue != 0 ? $", value={rule.ParamValue}" : string.Empty;
        return $"{rule.Type}(n={rule.ParamN}{value})";
    }

    private static string FormatAssets(IReadOnlyList<StakeAsset>? assets)
    {
        return assets == null || assets.Count == 0 ? "none" : string.Join(", ", assets.Select(FormatAsset));
    }

    private static string FormatAsset(StakeAsset asset)
    {
        return asset == null ? "null" : $"{asset.assetType}:{asset.stakeValue}";
    }

    private string FormatScores(Dictionary<Team, int> scores)
    {
        return scores.Count == 0
            ? "none"
            : string.Join(", ", scores.Select(pair => $"{TeamLabel(pair.Key)}={pair.Value}"));
    }

    private string FormatDistribution(Dictionary<Team, List<StakeAsset>> distribution)
    {
        return distribution.Count == 0
            ? "none"
            : string.Join("; ", distribution.Select(pair => $"{TeamLabel(pair.Key)} <= {FormatAssets(pair.Value)}"));
    }

    private static string JoinOrNone(IEnumerable<string> values)
    {
        string[] parts = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return parts.Length == 0 ? "none" : string.Join("; ", parts);
    }

    private static int IndexOf<T>(IReadOnlyList<T> values, T item)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(values[i], item))
                return i;
        }

        return -1;
    }
}
