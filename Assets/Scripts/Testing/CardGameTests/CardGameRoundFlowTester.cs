#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public sealed class CardGameRoundFlowTester : ISystemTester
{
    private const string TesterId = "card_game_round_flow_tester";

    private TestRegistration? _registration;

    public string GetTesterId()
    {
        return TesterId;
    }

    public void Initialize(TestRegistration registration)
    {
        _registration = registration;
    }

    public Task<TestResult> RunTests()
    {
        TestResult result = new TestResult
        {
            testId = _registration?.testId ?? "card_game_round_flow",
            testerId = GetTesterId(),
            success = true,
            message = string.Empty,
            testCases = new List<TestCaseResult>(),
            additionalData = new List<AdditionalDataItem>()
        };

        TestCaseResult testCase = RunRoundFlowTest();
        result.testCases.Add(testCase);
        result.success = testCase.passed;
        result.message = testCase.passed
            ? "CardGame round flow passed"
            : "CardGame round flow failed";

        return Task.FromResult(result);
    }

    private static TestCaseResult RunRoundFlowTest()
    {
        TestCaseResult result = new TestCaseResult
        {
            name = "CardGame advances through five betting rounds and can reset",
            passed = false,
            error = string.Empty,
            stackTrace = string.Empty
        };

        try
        {
            CardGame game = CreateGame();
            List<CardGame.GamePhase> phases = new List<CardGame.GamePhase>();
            List<int> startedBettingRounds = new List<int>();
            List<int> endedBettingRounds = new List<int>();
            List<int> dealtTableCardCounts = new List<int>();
            List<Skeleton> startedTurns = new List<Skeleton>();
            List<Skeleton> endedTurns = new List<Skeleton>();
            int startedRounds = 0;

            game.OnPhaseChanged += phases.Add;
            game.OnRoundStarted += _ => startedRounds++;
            game.OnBettingRoundStarted += round => startedBettingRounds.Add(round.BettingRound);
            game.OnBettingRoundEnded += round => endedBettingRounds.Add(round.BettingRound);
            game.OnTableCardsDealt += cards => dealtTableCardCounts.Add(cards.Count);
            game.OnTurnStarted += startedTurns.Add;
            game.OnTurnEnded += endedTurns.Add;

            AssertEqual(CardGame.GamePhase.DealingCards, game.phase, "Initial phase");

            game.DealPlayersCards();
            AssertEqual(CardGame.GamePhase.ShowingCombinations, game.phase, "Phase after dealing player cards");

            game.ShowCombinations();
            AssertEqual(CardGame.GamePhase.RoundStart, game.phase, "Phase after showing combinations");
            AssertNotNull(game.round, "Round should be created after showing combinations");

            game.StartRound();
            AssertEqual(CardGame.GamePhase.BettingRoundStart, game.phase, "Phase after starting round");
            AssertEqual(1, startedRounds, "Round start event count");

            for (int expectedRound = 0; expectedRound < 5; expectedRound++)
            {
                game.StartBettingRound();
                AssertEqual(CardGame.GamePhase.Betting, game.phase, $"Phase after starting betting round {expectedRound}");
                AssertEqual(expectedRound, game.round!.BettingRound, $"Betting round index {expectedRound}");

                CompleteCurrentBettingRound(game);

                AssertEqual(expectedRound + 1, game.round!.BettingRound, $"Betting round index after round {expectedRound}");

                if (expectedRound < 4)
                {
                    AssertEqual(CardGame.GamePhase.AddingCards, game.phase, $"Phase after betting round {expectedRound}");
                    int expectedCardsDealt = expectedRound == 0 ? 2 : 1;
                    game.DealTableCards();
                    AssertEqual(CardGame.GamePhase.BettingRoundStart, game.phase, $"Phase after dealing table cards {expectedRound}");
                    AssertEqual(expectedCardsDealt, dealtTableCardCounts[expectedRound], $"Dealt table cards after round {expectedRound}");
                }
                else
                {
                    AssertEqual(CardGame.GamePhase.End, game.phase, "Final phase after fifth betting round");
                }
            }

            AssertSequence(new[] { 0, 1, 2, 3, 4 }, startedBettingRounds, "Started betting rounds");
            AssertSequence(new[] { 1, 2, 3, 4, 5 }, endedBettingRounds, "Ended betting rounds");
            AssertEqual(5, game.round!.TableCards.Count, "Final table card count");
            AssertEqual(10, startedTurns.Count, "Turn started count");
            AssertEqual(10, endedTurns.Count, "Turn ended count");

            game.ResetRound();
            AssertEqual(CardGame.GamePhase.DealingCards, game.phase, "Phase after reset");
            AssertNull(game.round, "Round should be cleared after reset");

            game.DealPlayersCards();
            AssertEqual(CardGame.GamePhase.ShowingCombinations, game.phase, "A new round can start after reset");

            AssertSequence(
                new[]
                {
                    CardGame.GamePhase.ShowingCombinations,
                    CardGame.GamePhase.RoundStart,
                    CardGame.GamePhase.BettingRoundStart,
                    CardGame.GamePhase.Betting,
                    CardGame.GamePhase.AddingCards,
                    CardGame.GamePhase.BettingRoundStart,
                    CardGame.GamePhase.Betting,
                    CardGame.GamePhase.AddingCards,
                    CardGame.GamePhase.BettingRoundStart,
                    CardGame.GamePhase.Betting,
                    CardGame.GamePhase.AddingCards,
                    CardGame.GamePhase.BettingRoundStart,
                    CardGame.GamePhase.Betting,
                    CardGame.GamePhase.AddingCards,
                    CardGame.GamePhase.BettingRoundStart,
                    CardGame.GamePhase.Betting,
                    CardGame.GamePhase.End,
                    CardGame.GamePhase.DealingCards,
                    CardGame.GamePhase.ShowingCombinations
                },
                phases,
                "Phase sequence");

            result.passed = true;
        }
        catch (Exception ex)
        {
            result.error = ex.Message;
            result.stackTrace = ex.StackTrace ?? string.Empty;
        }

        return result;
    }

    private static CardGame CreateGame()
    {
        Team firstTeam = CreateTeam();
        Team secondTeam = CreateTeam();

        Skeleton firstPlayer = CreatePlayer(firstTeam);
        Skeleton secondPlayer = CreatePlayer(secondTeam);

        return new CardGame(
            new[] { firstTeam, secondTeam },
            new[] { firstPlayer, secondPlayer });
    }

    private static Team CreateTeam()
    {
        Team team = new Team();
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.Soul, 1));
        return team;
    }

    private static Skeleton CreatePlayer(Team team)
    {
        Skeleton player = new Skeleton(team);
        team.AddSkeleton(player);
        return player;
    }

    private static void CompleteCurrentBettingRound(CardGame game)
    {
        for (int turns = 0; turns < 8 && game.phase == CardGame.GamePhase.Betting; turns++)
        {
            CardGame.Round round = game.round ?? throw new InvalidOperationException("Round is missing");
            Skeleton player = round.CurrentPlayer;

            round.Call(player, Array.Empty<StakeAsset>(), DeclaredCombinationTier.Easy);
            round.EndTurn(player);
        }

        if (game.phase == CardGame.GamePhase.Betting)
            throw new InvalidOperationException("Betting round did not finish within the expected turn count");
    }

    private static void AssertEqual<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{label}: expected {expected}, actual {actual}");
    }

    private static void AssertNotNull(object? value, string label)
    {
        if (value == null)
            throw new InvalidOperationException(label);
    }

    private static void AssertNull(object? value, string label)
    {
        if (value != null)
            throw new InvalidOperationException(label);
    }

    private static void AssertSequence<T>(IEnumerable<T> expected, IEnumerable<T> actual, string label)
    {
        T[] expectedArray = expected.ToArray();
        T[] actualArray = actual.ToArray();

        if (!expectedArray.SequenceEqual(actualArray))
        {
            throw new InvalidOperationException(
                $"{label}: expected [{string.Join(", ", expectedArray)}], actual [{string.Join(", ", actualArray)}]");
        }
    }
}
