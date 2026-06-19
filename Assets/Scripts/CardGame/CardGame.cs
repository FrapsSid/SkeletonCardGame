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
            public IReadOnlyList<Team>? winners { get => result!.winners; }
            public RoundResult? result { get; private set; }
            public enum PlayerTurnState
            {
                NONE,
                RAISED,
                TAKEN_A_CARD
            }
            public Dictionary<Player, PlayerBetState> playerStates = new Dictionary<Player, PlayerBetState>();
            public Dictionary<Player, PlayerTurnState> playerTurnStates = new Dictionary<Player, PlayerTurnState>();
            public int currentParticipationPrice;
            public List<StakeAsset> currentPot = new List<StakeAsset>();
            private ISet<Player> activePlayers = new HashSet<Player>();
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
            }
            public bool CanRaise(Player player)
            {
                return playerTurnStates[player] != PlayerTurnState.TAKEN_A_CARD;
            }
            public bool CanTakeCard(Player player)
            {
                return playerTurnStates[player] != PlayerTurnState.RAISED;
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
                return playerStates[player].declaredTarget == null || newCombination >= playerStates[player].declaredTarget;
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
                return CanBet(player, assets.Sum(a => a.stakeValue), assets, combination);
            }
            public void Raise(Player player, IList<StakeAsset> assets, DeclaredCombinationTier combination)
            {
                Bet(player, assets.Sum(a => a.stakeValue), assets, combination);
            }
            public bool CanBet(Player player, int bet, IList<StakeAsset> assets, DeclaredCombinationTier combination)
            {
                if (bet < currentParticipationPrice)
                {
                    return false;
                }
                if (bet > currentParticipationPrice && !CanRaise(player))
                    return false;
                if (!CanUpdateCombination(player, combination))
                    return false;
                var assetsDiff = assets.Sum(a => a.stakeValue) - playerStates[player].AssetsValue;
                return assetsDiff + CalculateOverbet(player.team) >= bet - playerStates[player].committedValue;
            }
            public void Bet(Player player, int bet, IList<StakeAsset> assets, DeclaredCombinationTier combination)
            {
                CheckPlayersTurn(player);
                if (!CanBet(player, bet, assets, combination))
                    throw new System.Exception();
                var state = playerStates[player];
                state.committedValue = bet;
                state.committedAssets.Clear();
                foreach (StakeAsset asset in assets)
                {
                    state.committedAssets.Add(asset);
                }
                state.declaredTarget = combination;
                if (state.committedValue > currentParticipationPrice)
                {
                    currentParticipationPrice = state.committedValue;
                }
            }
            public void TakeCard(Player player)
            {
                CheckPlayersTurn(player);
                player.Hand.AddCard(game.deck.DrawCard());
                playerTurnStates[player] = PlayerTurnState.TAKEN_A_CARD;
            }
            public bool HasMatchedBet(Player player)
            {
                return playerStates[player].committedValue == currentParticipationPrice;
            }
            public void EndTurn(Player player)
            {
                CheckPlayersTurn(player);
                if (!HasMatchedBet(player))
                    throw new System.Exception();
                if (playerTurnStates[player] == PlayerTurnState.RAISED)
                    lastRaise = 0;
                do
                {
                    if (lastRaise++ == game.players.Count)
                    {
                        EndBettingRound();
                        return;
                    }
                    _currentPlayerIndex++;
                } while (!activePlayers.Contains(CurrentPlayer));
                BeginPlayersTurn(CurrentPlayer);
            }

            public void Fold(Player player)
            {
                CheckPlayersTurn(player);
            }
            private void BeginPlayersTurn(Player player)
            {
                playerTurnStates[player] = PlayerTurnState.NONE;
            }
            private void CheckPlayersTurn(Player player)
            {
                if (CurrentPlayer != player)
                {
                    throw new System.Exception("");
                }
            }
            public void StartBettingRound()
            {
                if (game.phase != GamePhase.BETTING_ROUND_START)
                {
                    throw new System.Exception();
                }
                game.phase = GamePhase.BETTING_ROUND;
                _currentPlayerIndex = 0;
                while (!activePlayers.Contains(CurrentPlayer))
                {
                    _currentPlayerIndex++;
                }
                BeginPlayersTurn(CurrentPlayer);
            }
            public void EndBettingRound()
            {
                bettingRound++;
                if (bettingRound == 4)
                {
                    game.phase = GamePhase.END;
                    DetermineWinners();
                }
                game.phase = GamePhase.ADDING_CARDS;
            }
            public void DealTableCards(int number)
            {
                for (int i = 0; i < number; i++)
                {
                    _tableCards.Add(game.deck.DrawCard());
                }
            }
            private void DetermineWinners()
            {
                if (game.phase != GamePhase.END)
                {
                    throw new System.Exception();
                }
                result = game.roundScorer.CalculateRoundResults(game.teams, tableCards.ToList(), combinations, playerStates);
            }
        }
        public enum GamePhase
        {
            DEALING_CARDS,
            SHOWING_COMBINATIONS,
            ROUND_START,
            BETTING_ROUND_START,
            BETTING_ROUND,
            ADDING_CARDS,
            END
        }
        public GamePhase phase { get; private set; } = GamePhase.DEALING_CARDS;
        public readonly Deck deck = new Deck();
        private List<Player> players;
        private List<Team> teams;
        public Round? round { get; private set; }
        public readonly CombinationGenerator combinationGenerator = new CombinationGenerator();
        public readonly RoundScorer roundScorer = new RoundScorer();

        public CardGame(IEnumerable<Team> teams, IEnumerable<Player> players)
        {
            this.teams = teams.ToList();
            this.players = players.ToList();
        }
        public void DealPlayersCards()
        {
            if (round != null || phase != GamePhase.DEALING_CARDS)
                throw new System.Exception("");
            deck.Reset();
            foreach (var player in players)
            {
                foreach (var card in deck.DrawCards(2))
                {
                    player.Hand.AddCard(card);
                }
            }
            phase = GamePhase.SHOWING_COMBINATIONS;
        }
        public void ShowCombinations()
        {
            if (round != null || phase != GamePhase.SHOWING_COMBINATIONS)
            {
                throw new System.Exception();
            }
            round = new Round(this, combinationGenerator.GenerateRoundCombinations());
            phase = GamePhase.ROUND_START;
        }
        public void StartRound()
        {
            if (round == null || phase != GamePhase.ROUND_START)
                throw new System.Exception("");
            phase = GamePhase.BETTING_ROUND_START;
            round.StartBettingRound();
        }
        public void DealTableCards()
        {
            if (round == null || phase != GamePhase.ADDING_CARDS)
            {
                throw new System.Exception();
            }
            if (round.bettingRound == 1)
            {
                round.DealTableCards(3);
            }
            if (round.bettingRound > 1 && round.bettingRound <= 3)
            {
                round.DealTableCards(1);
            }
            phase = GamePhase.BETTING_ROUND_START;
        }

        public void ResetRound()
        {
            if (phase != GamePhase.DEALING_CARDS && phase != GamePhase.END)
            {
                throw new System.Exception();
            }
            phase = GamePhase.DEALING_CARDS;
            deck.Reset();
            if (round != null)
                round = null;
        }
    }
}
