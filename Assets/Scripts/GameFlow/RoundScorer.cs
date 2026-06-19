using System.Collections.Generic;
using System.Linq;

public class RoundScorer
{
    public PlayerRoundEvaluation EvaluatePlayer(
        Skeleton player,
        List<CardData> tableCards,
        RoundCombinationSet combos,
        PlayerBetState betState)
    {
        DeclaredCombinationTier declaredTarget = betState != null && betState.declaredTarget != null
            ? (DeclaredCombinationTier)betState.declaredTarget
            : DeclaredCombinationTier.Easy;

        var evaluation = new PlayerRoundEvaluation(player, declaredTarget);

        if (player == null || combos == null || betState == null || betState.hasFolded)
        {
            evaluation.folded = true;
            evaluation.contributesToSharedPool = false;
            return evaluation;
        }

        List<CardData> personalPool = BuildPersonalPool(player, tableCards);

        if (combos.antiCombination != null && combos.antiCombination.IsSatisfied(personalPool))
        {
            evaluation.antiTriggered = true;
            evaluation.contributesToSharedPool = false;
            return evaluation;
        }

        Combination declaredCombination = combos.GetCombination(declaredTarget.ToCombinationDifficulty());
        if (declaredCombination == null || !declaredCombination.IsSatisfied(personalPool))
        {
            evaluation.declaredCombinationSatisfied = false;
            evaluation.contributesToSharedPool = false;
            return evaluation;
        }

        evaluation.declaredCombinationSatisfied = true;
        evaluation.contributesToSharedPool = true;
        AddUnique(evaluation.personalScoredCombinations, declaredCombination);

        return evaluation;
    }

    public int CalculateTeamScore(
        Team team,
        List<CardData> tableCards,
        RoundCombinationSet combos,
        Dictionary<Skeleton, PlayerBetState> playerStates)
    {
        TeamScoreDetails details = EvaluateTeam(team, tableCards, combos, playerStates);
        return details.Score;
    }

    public RoundResult CalculateRoundResults(
        List<Team> teams,
        List<CardData> tableCards,
        RoundCombinationSet combos,
        Dictionary<Skeleton, PlayerBetState> playerStates)
    {
        var result = new RoundResult();

        if (teams == null || combos == null)
        {
            return result;
        }

        foreach (Team team in teams)
        {
            if (team == null) continue;

            TeamScoreDetails details = EvaluateTeam(team, tableCards, combos, playerStates);
            result.scores[team] = details.Score;
            result.countedCombinations[team] = details.CountedCombinations;

            foreach (KeyValuePair<Skeleton, PlayerRoundEvaluation> pair in details.Evaluations)
            {
                result.playerEvaluations[pair.Key] = pair.Value;
            }
        }

        if (result.scores.Count == 0)
        {
            return result;
        }

        int maxScore = result.scores.Values.Max();
        result.winners = result.scores
            .Where(pair => pair.Value == maxScore)
            .Select(pair => pair.Key)
            .ToList();

        return result;
    }

    private TeamScoreDetails EvaluateTeam(
        Team team,
        List<CardData> tableCards,
        RoundCombinationSet combos,
        Dictionary<Skeleton, PlayerBetState> playerStates)
    {
        var details = new TeamScoreDetails();
        if (team == null || team.Skeletons == null || combos == null) return details;

        foreach (Skeleton player in team.Skeletons)
        {
            if (player == null) continue;

            PlayerBetState betState = null;
            playerStates?.TryGetValue(player, out betState);

            PlayerRoundEvaluation evaluation = EvaluatePlayer(player, tableCards, combos, betState);
            details.Evaluations[player] = evaluation;
        }

        List<PlayerRoundEvaluation> eligibleEvaluations = details.Evaluations.Values
            .Where(evaluation => evaluation.contributesToSharedPool)
            .ToList();

        foreach ((Combination combination, CombinationDifficulty difficulty) in combos.GetScoringCombinations())
        {
            if (combination == null) continue;

            bool counted = TryCountIndividualCombination(eligibleEvaluations, tableCards, combination, difficulty);

            if (!counted)
            {
                counted = CanSharedPoolScore(eligibleEvaluations, tableCards, combination, difficulty);
            }

            if (counted)
            {
                AddUnique(details.CountedCombinations, combination);
                details.Score += difficulty.GetScoreValue();
            }
        }

        return details;
    }

    private bool TryCountIndividualCombination(
        List<PlayerRoundEvaluation> eligibleEvaluations,
        List<CardData> tableCards,
        Combination combination,
        CombinationDifficulty difficulty)
    {
        foreach (PlayerRoundEvaluation evaluation in eligibleEvaluations)
        {
            if (!evaluation.declaredTarget.AllowsDifficulty(difficulty)) continue;

            List<CardData> personalPool = BuildPersonalPool(evaluation.player, tableCards);
            if (!combination.IsSatisfied(personalPool)) continue;

            AddUnique(evaluation.personalScoredCombinations, combination);
            return true;
        }

        return false;
    }

    private bool CanSharedPoolScore(
        List<PlayerRoundEvaluation> eligibleEvaluations,
        List<CardData> tableCards,
        Combination combination,
        CombinationDifficulty difficulty)
    {
        if (eligibleEvaluations.Count == 0) return false;

        for (int subsetSize = 1; subsetSize <= eligibleEvaluations.Count; subsetSize++)
        {
            var subset = new List<PlayerRoundEvaluation>(subsetSize);
            if (CanAnyEligibleSubsetScore(eligibleEvaluations, 0, subsetSize, subset, tableCards, combination, difficulty))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanAnyEligibleSubsetScore(
        List<PlayerRoundEvaluation> source,
        int startIndex,
        int targetSize,
        List<PlayerRoundEvaluation> currentSubset,
        List<CardData> tableCards,
        Combination combination,
        CombinationDifficulty difficulty)
    {
        if (currentSubset.Count == targetSize)
        {
            DeclaredCombinationTier weakestDeclaredTier = (DeclaredCombinationTier)currentSubset
                .Select(evaluation => evaluation.declaredTarget)
                .Min(tier => (int)tier);

            if (!weakestDeclaredTier.AllowsDifficulty(difficulty))
            {
                return false;
            }

            List<CardData> sharedPool = BuildSharedPool(currentSubset, tableCards);
            return combination.IsSatisfied(sharedPool);
        }

        int remainingNeeded = targetSize - currentSubset.Count;
        if (source.Count - startIndex < remainingNeeded) return false;

        for (int i = startIndex; i < source.Count; i++)
        {
            currentSubset.Add(source[i]);

            if (CanAnyEligibleSubsetScore(source, i + 1, targetSize, currentSubset, tableCards, combination, difficulty))
            {
                return true;
            }

            currentSubset.RemoveAt(currentSubset.Count - 1);
        }

        return false;
    }

    private List<CardData> BuildPersonalPool(Skeleton player, List<CardData> tableCards)
    {
        var pool = new List<CardData>();

        if (player?.Hand != null)
        {
            pool.AddRange(player.Hand.GetCards());
        }

        if (tableCards != null)
        {
            pool.AddRange(tableCards);
        }

        return pool;
    }

    private List<CardData> BuildSharedPool(List<PlayerRoundEvaluation> evaluations, List<CardData> tableCards)
    {
        var pool = new List<CardData>();

        foreach (PlayerRoundEvaluation evaluation in evaluations)
        {
            if (evaluation.player?.Hand != null)
            {
                pool.AddRange(evaluation.player.Hand.GetCards());
            }
        }

        if (tableCards != null)
        {
            pool.AddRange(tableCards);
        }

        return pool;
    }

    private void AddUnique(List<Combination> combinations, Combination combination)
    {
        if (combination != null && !combinations.Contains(combination))
        {
            combinations.Add(combination);
        }
    }

    private class TeamScoreDetails
    {
        public int Score;
        public List<Combination> CountedCombinations = new List<Combination>();
        public Dictionary<Skeleton, PlayerRoundEvaluation> Evaluations = new Dictionary<Skeleton, PlayerRoundEvaluation>();
    }
}
