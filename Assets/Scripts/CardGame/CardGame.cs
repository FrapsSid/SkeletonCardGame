using System;
using System.Collections.Generic;
using System.Linq;
#nullable enable
namespace Assets.Scripts.CardGame
{
    using Player = Skeleton;

    public class CardGame
    {
        public class Round
        {
            private int _currentPlayerIndex = 0;
            public int bettingRound { get; private set; } = 0;
            public Player CurrentPlayer { get => game.players[_currentPlayerIndex]; }
            public IReadOnlyList<Team>? winners { get => result?.winners; }
            public RoundResult? result { get; private set; }
            public enum PlayerTurnState
            {
                None,
                Raised,
                TakenACard
            }
            public Dictionary<Player, PlayerBetState> playerStates = new Dictionary<Player, PlayerBetState>();
            public Dictionary<Player, PlayerTurnState> playerTurnStates = new Dictionary<Player, PlayerTurnState>();
            public int currentParticipationPrice;
            private readonly HashSet<Player> activePlayers = new HashSet<Player>();
            public IReadOnlyCollection<Player> ActivePlayers => activePlayers;
            private CardGame game;
            public int lastRaise = 0;
            private List<CardData> _tableCards = new List<CardData>();
            public readonly IReadOnlyList<CardData> tableCards;
            public readonly RoundCombinationSet combinations;

            public Round(CardGame cardGame, RoundCombinationSet combinations)
            {
                this.game = cardGame;
                tableCards = _tableCards.AsReadOnly();
                this.combinations = combinations;

                foreach (var player in game.players)
                {
                    playerStates[player] = new PlayerBetState();
                    playerTurnStates[player] = PlayerTurnState.None;
                    activePlayers.Add(player);
                }
            }
            public bool CanRaise(Player player)
            {
                return IsBettingPhase()
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
                    game.RaiseTargetDeclared(player, combination);
                else if (combination > oldTarget.Value)
                    game.RaiseTargetUpgraded(player, combination);

                if (state.committedValue > currentParticipationPrice)
                {
                    currentParticipationPrice = state.committedValue;
                    playerTurnStates[player] = PlayerTurnState.Raised;
                    lastRaise = 0;
                    game.RaisePriceRaised(player, currentParticipationPrice);
                }
                else
                {
                    game.RaisePriceMatched(player, currentParticipationPrice);
                }
            }
            public void TakeCard(Player player)
            {
                EnsureBettingPhase();
                CheckPlayersTurn(player);
                CardData card = game.deck.DrawCard();
                player.Hand.AddCard(card);
                playerTurnStates[player] = PlayerTurnState.TakenACard;
                game.RaiseCardTaken(player, card);
            }
            public bool HasMatchedBet(Player player)
            {
                return playerStates[player].committedValue == currentParticipationPrice;
            }
            public void EndTurn(Player player)
            {
                EnsureBettingPhase();
                CheckPlayersTurn(player);
                if (!HasMatchedBet(player))
                    throw new InvalidOperationException("Player has not matched the current price");

                if (playerTurnStates[player] == PlayerTurnState.Raised)
                    lastRaise = 0;

                game.RaiseTurnEnded(player);
                MoveToNextPlayerOrEndBettingRound();
            }

            public void Fold(Player player)
            {
                EnsureBettingPhase();
                CheckPlayersTurn(player);

                if (activePlayers.Count <= 1)
                    throw new InvalidOperationException("Last active player cannot fold");

                PlayerBetState state = playerStates[player];
                state.hasFolded = true;
                activePlayers.Remove(player);

                game.RaisePlayerFolded(player);
                game.RaiseTurnEnded(player);

                if (activePlayers.Count <= 1)
                {
                    bettingRound++;
                    game.RaiseBettingRoundEnded(this);
                    game.phase = GamePhase.End;
                    return;
                }

                MoveToNextActivePlayer();
                BeginPlayersTurn(CurrentPlayer);
            }
            public void ResolvePot()
            {
                if (game.phase != GamePhase.End)
                    throw new InvalidOperationException("Pot can only be resolved at round end");
                if (result == null)
                    throw new InvalidOperationException("Winners must be determined before resolving the pot");
                if (result.assetDistribution.Count == 0)
                    throw new InvalidOperationException("Asset distribution must be calculated before resolving the pot");

                List<StakeAsset> resolvedAssets = new List<StakeAsset>();
                foreach (var pair in result.assetDistribution)
                {
                    foreach (StakeAsset asset in pair.Value)
                    {
                        asset.TransferOwnership(pair.Key);
                        resolvedAssets.Add(asset);
                    }
                }

                game.RaisePotResolved(result.winners, resolvedAssets);
            }
            private void BeginPlayersTurn(Player player)
            {
                playerTurnStates[player] = PlayerTurnState.None;
                game.RaiseTurnStarted(player);
            }
            private void CheckPlayersTurn(Player player)
            {
                if (player == null)
                    throw new ArgumentNullException(nameof(player));
                if (activePlayers.Count == 0)
                    throw new InvalidOperationException("No active players");
                if (_currentPlayerIndex < 0 || _currentPlayerIndex >= game.players.Count || CurrentPlayer != player)
                    throw new InvalidOperationException("Not player's turn");
            }
            private bool IsBettingPhase()
            {
                return game.phase == GamePhase.Betting;
            }
            private void EnsureBettingPhase()
            {
                if (!IsBettingPhase())
                    throw new InvalidOperationException("Betting action is only available during betting phase");
            }
            public void StartBettingRound()
            {
                if (game.phase != GamePhase.BettingRoundStart)
                    throw new InvalidOperationException("Betting round cannot start in the current phase");

                game.phase = GamePhase.Betting;
                game.RaiseBettingRoundStarted(this);
                lastRaise = 0;

                if (activePlayers.Count == 0)
                {
                    EndBettingRound();
                    return;
                }

                _currentPlayerIndex = game.players.Count - 1;
                MoveToNextActivePlayer();
                BeginPlayersTurn(CurrentPlayer);
            }
            public void EndBettingRound()
            {
                bettingRound++;
                game.RaiseBettingRoundEnded(this);

                if (activePlayers.Count <= 1)
                {
                    game.phase = GamePhase.End;
                    return;
                }

                if (bettingRound == 4)
                {
                    game.phase = GamePhase.End;
                    return;
                }

                game.phase = GamePhase.AddingCards;
            }
            public void DealTableCards(int number)
            {
                List<CardData> dealtCards = new List<CardData>();
                for (int i = 0; i < number; i++)
                {
                    CardData card = game.deck.DrawCard();
                    _tableCards.Add(card);
                    dealtCards.Add(card);
                }

                game.RaiseTableCardsDealt(dealtCards);
            }
            private void MoveToNextPlayerOrEndBettingRound()
            {
                if (activePlayers.Count == 0)
                {
                    EndBettingRound();
                    return;
                }
                if (lastRaise++ >= activePlayers.Count - 1)
                {
                    EndBettingRound();
                    return;
                }

                MoveToNextActivePlayer();
                BeginPlayersTurn(CurrentPlayer);
            }
            private void MoveToNextActivePlayer()
            {
                for (int i = 0; i < game.players.Count; i++)
                {
                    _currentPlayerIndex = (_currentPlayerIndex + 1) % game.players.Count;
                    if (activePlayers.Contains(CurrentPlayer))
                        return;
                }

                throw new InvalidOperationException("No active players");
            }
            public void DetermineWinners()
            {
                if (game.phase != GamePhase.End)
                    throw new InvalidOperationException("Winners can only be determined at round end");
                result = game.roundScorer.CalculateRoundResults(game.teams, tableCards.ToList(), combinations, playerStates);
                result.assetDistribution = SplitPotBetweenWinners(GetCommittedAssets(), result.winners);
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
        public readonly CombinationGenerator combinationGenerator = new CombinationGenerator();
        public readonly RoundScorer roundScorer = new RoundScorer();

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

        public CardGame(IEnumerable<Team> teams, IEnumerable<Player> players)
        {
            this.teams = teams.ToList();
            this.players = players.ToList();
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
            round = new Round(this, combinationGenerator.GenerateRoundCombinations());
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
            if (round.bettingRound == 1)
                round.DealTableCards(3);
            if (round.bettingRound > 1 && round.bettingRound <= 3)
                round.DealTableCards(1);
            phase = GamePhase.BettingRoundStart;
        }

        public void ResetRound()
        {
            if (phase != GamePhase.DealingCards && phase != GamePhase.End)
                throw new InvalidOperationException();
            phase = GamePhase.DealingCards;
            deck.Reset();
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
        }
        private void RaiseTurnEnded(Player player)
        {
            OnTurnEnded?.Invoke(player);
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
}
