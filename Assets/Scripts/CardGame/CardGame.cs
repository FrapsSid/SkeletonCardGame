using System;
using System.Collections.Generic;
using System.Linq;
using Combinations;
#nullable enable

using Player = Skeleton;

/*
 External game flow:
 1. Subscribe to events before starting the round flow. Every phase assignment raises OnPhaseChanged.
 2. Call DealPlayersCards(): deals private cards and moves to ShowingCombinations.
 3. Call ShowCombinations(): creates round and moves to RoundStart.
 4. Call StartRound(): moves to BettingRoundStart and raises OnRoundStarted.
 5. Call StartBettingRound(): moves to Betting, raises OnBettingRoundStarted,
    then OnTurnStarted for the first active player.
 6. During Betting, external input should call round.Call/Raise/AllIn/TakeCard/Fold/EndTurn for CurrentPlayer.
    These methods can raise OnTargetDeclared, OnTargetUpgraded, OnPriceMatched, OnPriceRaised,
    OnCardTaken, OnPlayerFolded, OnTurnEnded, and OnTurnStarted as turns advance.
    An all-in team no longer has to match future bets, but its players still receive turns,
    can take cards or fold, and participate in scoring while active.
 7. When a betting round ends, OnBettingRoundEnded is raised. If phase becomes AddingCards,
    call DealTableCards(): it deals table cards, raises OnTableCardsDealt, and moves to BettingRoundStart.
    Then call StartBettingRound() again.
 8. If phase becomes End, no winners or assets are resolved automatically. External code must call
    round.DetermineWinners(), then round.ResolvePot(). ResolvePot transfers assets, raises
    OnPotResolved, then raises OnRoundEnded.
 9. Call ResetRound() from DealingCards or End to return to DealingCards for a new round.
 */
public class CardGame
{
    public sealed class PlayerTurnEvent
    {
        public event Action<Player>? Fired;

        internal void Invoke(Player player)
        {
            Fired?.Invoke(player);
        }
    }

    public class Round
    {
        private int _currentPlayerIndex = 0;
        public int BettingRound { get; private set; } = 0;
        public Player CurrentPlayer => _game.players[_currentPlayerIndex];
        public IReadOnlyList<Team>? Winners => Result?.winners;
        public RoundResult? Result { get; private set; }
        public enum PlayerTurnState
        {
            None,
            Raised,
            TakenACard
        }
        public Dictionary<Player, PlayerBetState> playerStates = new();
        public Dictionary<Player, PlayerTurnState> playerTurnStates = new();
        public int currentParticipationPrice { get; private set; }
        private readonly HashSet<Player> _activePlayers = new();
        private readonly HashSet<Team> _allInTeams = new();
        public IReadOnlyCollection<Player> ActivePlayers => _activePlayers;
        private readonly CardGame _game;
        private int _lastRaise = 0;
        private readonly List<CardData> _tableCards = new();
        public readonly IReadOnlyList<CardData> TableCards;
        public readonly RoundCombinationSet Combinations;

        public Round(CardGame cardGame, RoundCombinationSet combinations)
        {
            this._game = cardGame;
            TableCards = _tableCards.AsReadOnly();
            this.Combinations = combinations;

            foreach (var player in _game.players)
            {
                playerStates[player] = new PlayerBetState();
                playerTurnStates[player] = PlayerTurnState.None;
                _activePlayers.Add(player);
            }
        }
        public bool CanRaise(Player player)
        {
            return IsBettingPhase()
                && !IsTeamAllIn(player.team)
                && playerTurnStates[player] != PlayerTurnState.TakenACard;
        }
        public bool CanTakeCard(Player player)
        {
            return IsBettingPhase()
                && playerTurnStates[player] != PlayerTurnState.Raised;
        }
        public int CalculateOverbet(Team team)
        {
            int overbet = 0;
            foreach (var player in team.Skeletons)
            {
                var state = playerStates[player];
                overbet += state.AssetsValue - state.committedValue;
            }
            return overbet;
        }
        public bool CanUpdateCombination(Player player, DeclaredCombinationTier newCombination)
        {
            var state = playerStates[player];
            return state.declaredTarget == null || newCombination >= state.declaredTarget;
        }
        public bool CanCall(Player player, IList<StakeAsset> assets, DeclaredCombinationTier combination)
        {
            return CanBet(player, currentParticipationPrice, assets, combination);
        }
        public void Call(Player player, IList<StakeAsset> assets, DeclaredCombinationTier combination)
        {
            Bet(player, currentParticipationPrice, assets, combination);
        }
        public bool CanRaise(Player player, IList<StakeAsset> assets, DeclaredCombinationTier combination)
        {
            return CanBet(player, CalculateStakeValue(assets), assets, combination);
        }
        public void Raise(Player player, IList<StakeAsset> assets, DeclaredCombinationTier combination)
        {
            Bet(player, CalculateStakeValue(assets), assets, combination);
        }
        public bool CanBet(Player player, int bet, IList<StakeAsset> assets, DeclaredCombinationTier combination)
        {
            if (!IsBettingPhase())
                return false;
            if (IsTeamAllIn(player.team))
                return false;

            if (bet < currentParticipationPrice)
                return false;
            if (bet > currentParticipationPrice && !CanRaise(player))
                return false;
            if (!CanUpdateCombination(player, combination))
                return false;
            var assetsDiff = CalculateStakeValue(assets) - playerStates[player].AssetsValue;
            return assetsDiff + CalculateOverbet(player.team) >= bet - playerStates[player].committedValue;
        }
        public void Bet(Player player, int bet, IList<StakeAsset> assets, DeclaredCombinationTier combination)
        {
            EnsureBettingPhase();
            CheckPlayersTurn(player);
            if (!CanBet(player, bet, assets, combination))
                throw new InvalidOperationException("Invalid bet");

            var state = playerStates[player];
            DeclaredCombinationTier? oldTarget = state.declaredTarget;

            state.committedValue = bet;
            state.committedAssets.Clear();
            foreach (StakeAsset asset in assets)
            {
                state.committedAssets.Add(asset);
            }

            state.declaredTarget = combination;
            if (oldTarget == null)
                _game.RaiseTargetDeclared(player, combination);
            else if (combination > oldTarget.Value)
                _game.RaiseTargetUpgraded(player, combination);

            if (state.committedValue > currentParticipationPrice)
            {
                currentParticipationPrice = state.committedValue;
                playerTurnStates[player] = PlayerTurnState.Raised;
                _lastRaise = 0;
                _game.RaisePriceRaised(player, currentParticipationPrice);
            }
            else
            {
                _game.RaisePriceMatched(player, currentParticipationPrice);
            }
        }
        public bool IsTeamAllIn(Team team)
        {
            return _allInTeams.Contains(team);
        }
        public bool CanAllIn(Player player, DeclaredCombinationTier combination)
        {
            if (!IsBettingPhase())
                return false;
            if (CurrentPlayer != player)
                return false;
            if (!_activePlayers.Contains(player))
                return false;
            if (IsTeamAllIn(player.team))
                return false;
            return CanUpdateCombination(player, combination);
        }
        public void AllIn(Player player, DeclaredCombinationTier combination)
        {
            EnsureBettingPhase();
            CheckPlayersTurn(player);
            if (!CanAllIn(player, combination))
                throw new InvalidOperationException("Invalid all-in");

            var state = playerStates[player];
            DeclaredCombinationTier? oldTarget = state.declaredTarget;
            List<StakeAsset> assets = player.team.Assets.ToList();

            state.committedValue = CalculateStakeValue(assets);
            state.committedAssets.Clear();
            foreach (StakeAsset asset in assets)
            {
                state.committedAssets.Add(asset);
            }

            state.declaredTarget = combination;
            _allInTeams.Add(player.team);

            if (oldTarget == null)
                _game.RaiseTargetDeclared(player, combination);
            else if (combination > oldTarget.Value)
                _game.RaiseTargetUpgraded(player, combination);

            if (state.committedValue > currentParticipationPrice)
            {
                currentParticipationPrice = state.committedValue;
                playerTurnStates[player] = PlayerTurnState.Raised;
                _lastRaise = 0;
                _game.RaisePriceRaised(player, currentParticipationPrice);
            }
            else
            {
                _game.RaisePriceMatched(player, currentParticipationPrice);
            }
        }
        public void TakeCard(Player player)
        {
            EnsureBettingPhase();
            CheckPlayersTurn(player);
            CardData card = _game.deck.DrawCard();
            player.Hand.AddCard(card);
            playerTurnStates[player] = PlayerTurnState.TakenACard;
            _game.RaiseCardTaken(player, card);
        }
        public bool HasMatchedBet(Player player)
        {
            return IsTeamAllIn(player.team) || playerStates[player].committedValue == currentParticipationPrice;
        }
        public void EndTurn(Player player)
        {
            EnsureBettingPhase();
            CheckPlayersTurn(player);
            if (!HasMatchedBet(player))
                throw new InvalidOperationException("Player has not matched the current price");

            if (playerTurnStates[player] == PlayerTurnState.Raised)
                _lastRaise = 0;

            _game.RaiseTurnEnded(player);
            MoveToNextPlayerOrEndBettingRound();
        }

        public void Fold(Player player)
        {
            EnsureBettingPhase();
            CheckPlayersTurn(player);

            if (_activePlayers.Count <= 1)
                throw new InvalidOperationException("Last active player cannot fold");
            PlayerBetState state = playerStates[player];
            state.hasFolded = true;
            _activePlayers.Remove(player);

            _game.RaisePlayerFolded(player);
            _game.RaiseTurnEnded(player);

            if (_activePlayers.Count <= 1)
            {
                BettingRound++;
                _game.RaiseBettingRoundEnded(this);
                _game.phase = GamePhase.End;
                return;
            }

            MoveToNextActivePlayer();
            BeginPlayersTurn(CurrentPlayer);
        }
        public void ResolvePot()
        {
            if (_game.phase != GamePhase.End)
                throw new InvalidOperationException("Pot can only be resolved at round end");
            if (Result == null)
                throw new InvalidOperationException("Winners must be determined before resolving the pot");
            if (Result.assetDistribution.Count == 0)
                throw new InvalidOperationException("Asset distribution must be calculated before resolving the pot");

            List<StakeAsset> resolvedAssets = new List<StakeAsset>();
            foreach (var pair in Result.assetDistribution)
            {
                foreach (StakeAsset asset in pair.Value)
                {
                    asset.TransferOwnership(pair.Key);
                    resolvedAssets.Add(asset);
                }
            }

            _game.RaisePotResolved(Result.winners, resolvedAssets);
            _game.RaiseRoundEnded(Result);
        }
        private void BeginPlayersTurn(Player player)
        {
            playerTurnStates[player] = PlayerTurnState.None;
            _game.RaiseTurnStarted(player);
        }
        private void CheckPlayersTurn(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (_activePlayers.Count == 0)
                throw new InvalidOperationException("No active players");
            if (_currentPlayerIndex < 0 || _currentPlayerIndex >= _game.players.Count || CurrentPlayer != player)
                throw new InvalidOperationException("Not player's turn");
        }
        private bool IsBettingPhase()
        {
            return _game.phase == GamePhase.Betting;
        }
        private void EnsureBettingPhase()
        {
            if (!IsBettingPhase())
                throw new InvalidOperationException("Betting action is only available during betting phase");
        }
        public void StartBettingRound()
        {
            if (_game.phase != GamePhase.BettingRoundStart)
                throw new InvalidOperationException("Betting round cannot start in the current phase");

            _game.phase = GamePhase.Betting;
            _game.RaiseBettingRoundStarted(this);
            _lastRaise = 0;

            if (_activePlayers.Count == 0)
            {
                EndBettingRound();
                return;
            }

            _currentPlayerIndex = _game.players.Count - 1;
            MoveToNextActivePlayer();
            BeginPlayersTurn(CurrentPlayer);
        }
        public void EndBettingRound()
        {
            BettingRound++;
            _game.RaiseBettingRoundEnded(this);

            if (_activePlayers.Count <= 1)
            {
                _game.phase = GamePhase.End;
                return;
            }

            if (BettingRound == 5)
            {
                _game.phase = GamePhase.End;
                return;
            }

            _game.phase = GamePhase.AddingCards;
        }
        public void DealTableCards(int number)
        {
            List<CardData> dealtCards = new List<CardData>();
            for (int i = 0; i < number; i++)
            {
                CardData card = _game.deck.DrawCard();
                _tableCards.Add(card);
                dealtCards.Add(card);
            }

            _game.RaiseTableCardsDealt(dealtCards);
        }
        private void MoveToNextPlayerOrEndBettingRound()
        {
            if (_activePlayers.Count == 0)
            {
                EndBettingRound();
                return;
            }
            if (_lastRaise++ >= _activePlayers.Count - 1)
            {
                EndBettingRound();
                return;
            }

            MoveToNextActivePlayer();
            BeginPlayersTurn(CurrentPlayer);
        }
        private void MoveToNextActivePlayer()
        {
            for (int i = 0; i < _game.players.Count; i++)
            {
                _currentPlayerIndex = (_currentPlayerIndex + 1) % _game.players.Count;
                if (_activePlayers.Contains(CurrentPlayer))
                    return;
            }

            throw new InvalidOperationException("No active players");
        }
        public void DetermineWinners()
        {
            if (_game.phase != GamePhase.End)
                throw new InvalidOperationException("Winners can only be determined at round end");
            List<Team> activeTeams = _game.teams
                .Where(team => team != null && team.Skeletons.Any(_activePlayers.Contains))
                .ToList();
            Result = _game.RoundScorer.CalculateRoundResults(activeTeams, TableCards.ToList(), Combinations, playerStates);
            Result.assetDistribution = SplitPotBetweenWinners(GetCommittedAssets(), Result.winners);
        }

        private List<StakeAsset> GetCommittedAssets()
        {
            return playerStates
                .Values
                .SelectMany(state => state.committedAssets)
                .Distinct()
                .ToList();
        }

        private static int CalculateStakeValue(IList<StakeAsset> assets)
        {
            int value = 0;
            foreach (var asset in assets)
            {
                if (asset != null)
                    value += asset.stakeValue;
            }
            return value;
        }
        private static Dictionary<Team, List<StakeAsset>> SplitPotBetweenWinners(List<StakeAsset> assets, List<Team> winners)
        {
            var assignedValues = new Dictionary<Team, int>();
            var distribution = new Dictionary<Team, List<StakeAsset>>();
            foreach (var winner in winners)
            {
                assignedValues[winner] = 0;
                distribution[winner] = new List<StakeAsset>();
            }

            foreach (var asset in assets.OrderByDescending(asset => asset != null ? asset.stakeValue : 0))
            {
                if (asset == null)
                    continue;

                var targetWinner = assignedValues
                    .OrderBy(pair => pair.Value)
                    .ThenBy(pair => winners.IndexOf(pair.Key))
                    .First()
                    .Key;

                distribution[targetWinner].Add(asset);
                assignedValues[targetWinner] += Math.Max(0, asset.stakeValue);
            }

            return distribution;
        }
    }
    public enum GamePhase
    {
        DealingCards,
        ShowingCombinations,
        RoundStart,
        BettingRoundStart,
        Betting,
        AddingCards,
        End
    }
    private GamePhase _phase = GamePhase.DealingCards;
    public GamePhase phase
    {
        get => _phase;
        private set
        {
            _phase = value;
            OnPhaseChanged?.Invoke(value);
        }
    }
    public readonly Deck deck = new Deck();
    private List<Player> players;
    private List<Team> teams;
    public Round? round { get; private set; }
    public readonly CombinationGenerator CombinationGenerator = new();
    public readonly RoundScorer RoundScorer = new();

    public event Action<GamePhase>? OnPhaseChanged;
    public event Action<Round>? OnRoundStarted;
    public event Action<Round>? OnBettingRoundStarted;
    public event Action<Round>? OnBettingRoundEnded;
    public event Action<RoundResult>? OnRoundEnded;
    public event Action<Player, DeclaredCombinationTier>? OnTargetDeclared;
    public event Action<Player, DeclaredCombinationTier>? OnTargetUpgraded;
    public event Action<Player, int>? OnPriceMatched;
    public event Action<Player, int>? OnPriceRaised;
    public event Action<Player>? OnPlayerFolded;
    public event Action<Player>? OnTurnStarted;
    public event Action<Player>? OnTurnEnded;
    public event Action<Player, CardData>? OnCardTaken;
    public event Action<IReadOnlyList<CardData>>? OnTableCardsDealt;
    public event Action<IReadOnlyList<Team>, IReadOnlyList<StakeAsset>>? OnPotResolved;
    public IReadOnlyDictionary<Player, PlayerTurnEvent> TurnStartedByPlayer => _turnStartedByPlayer;
    public IReadOnlyDictionary<Player, PlayerTurnEvent> TurnEndedByPlayer => _turnEndedByPlayer;

    private readonly Dictionary<Player, PlayerTurnEvent> _turnStartedByPlayer = new();
    private readonly Dictionary<Player, PlayerTurnEvent> _turnEndedByPlayer = new();

    public CardGame(IEnumerable<Team> teams, IEnumerable<Player> players)
    {
        this.teams = teams.ToList();
        this.players = players.ToList();
        foreach (Player player in this.players)
        {
            _turnStartedByPlayer[player] = new PlayerTurnEvent();
            _turnEndedByPlayer[player] = new PlayerTurnEvent();
        }
    }
    public void DealPlayersCards()
    {
        if (round != null || phase != GamePhase.DealingCards)
            throw new InvalidOperationException("");
        deck.Reset();
        foreach (var player in players)
        {
            foreach (var card in deck.DrawCards(2))
            {
                player.Hand.AddCard(card);
            }
        }
        phase = GamePhase.ShowingCombinations;
    }
    public void ShowCombinations()
    {
        if (round != null || phase != GamePhase.ShowingCombinations)
            throw new InvalidOperationException();
        round = new Round(this, CombinationGenerator.GenerateRoundCombinations());
        phase = GamePhase.RoundStart;
    }
    public void StartRound()
    {
        if (round == null || phase != GamePhase.RoundStart)
            throw new InvalidOperationException("");

        phase = GamePhase.BettingRoundStart;
        RaiseRoundStarted(round);
    }
    public void StartBettingRound()
    {
        if (round == null || phase != GamePhase.BettingRoundStart)
            throw new InvalidOperationException("");

        round.StartBettingRound();
    }
    public void DealTableCards()
    {
        if (round == null || phase != GamePhase.AddingCards)
            throw new InvalidOperationException();
        if (round.BettingRound == 1)
            round.DealTableCards(2);
        if (round.BettingRound > 1 && round.BettingRound <= 4)
            round.DealTableCards(1);
        phase = GamePhase.BettingRoundStart;
    }

    public void ResetRound()
    {
        if (phase != GamePhase.DealingCards && phase != GamePhase.End)
            throw new InvalidOperationException();
        phase = GamePhase.DealingCards;
        deck.Reset();
        foreach (var player in players)
        {
            foreach (var card in player.Hand.GetCards())
            {
                player.Hand.RemoveCard(card);
            }
        }
        if (round != null)
            round = null;
    }
    private void RaiseRoundStarted(Round startedRound)
    {
        OnRoundStarted?.Invoke(startedRound);
    }
    private void RaiseBettingRoundStarted(Round startedRound)
    {
        OnBettingRoundStarted?.Invoke(startedRound);
    }
    private void RaiseBettingRoundEnded(Round endedRound)
    {
        OnBettingRoundEnded?.Invoke(endedRound);
    }
    private void RaiseRoundEnded(RoundResult result)
    {
        OnRoundEnded?.Invoke(result);
    }
    private void RaiseTargetDeclared(Player player, DeclaredCombinationTier target)
    {
        OnTargetDeclared?.Invoke(player, target);
    }
    private void RaiseTargetUpgraded(Player player, DeclaredCombinationTier target)
    {
        OnTargetUpgraded?.Invoke(player, target);
    }
    private void RaisePriceMatched(Player player, int price)
    {
        OnPriceMatched?.Invoke(player, price);
    }
    private void RaisePriceRaised(Player player, int price)
    {
        OnPriceRaised?.Invoke(player, price);
    }
    private void RaisePlayerFolded(Player player)
    {
        OnPlayerFolded?.Invoke(player);
    }
    private void RaiseTurnStarted(Player player)
    {
        OnTurnStarted?.Invoke(player);
        if (_turnStartedByPlayer.TryGetValue(player, out PlayerTurnEvent turnEvent))
            turnEvent.Invoke(player);
    }
    private void RaiseTurnEnded(Player player)
    {
        OnTurnEnded?.Invoke(player);
        if (_turnEndedByPlayer.TryGetValue(player, out PlayerTurnEvent turnEvent))
            turnEvent.Invoke(player);
    }
    private void RaiseCardTaken(Player player, CardData card)
    {
        OnCardTaken?.Invoke(player, card);
    }
    private void RaiseTableCardsDealt(IReadOnlyList<CardData> cards)
    {
        OnTableCardsDealt?.Invoke(cards);
    }
    private void RaisePotResolved(IReadOnlyList<Team> winners, IReadOnlyList<StakeAsset> assets)
    {
        OnPotResolved?.Invoke(winners, assets);
    }

}
