using System.Collections.Generic;

public class RoundResult
{
    public Dictionary<Team, int> scores = new Dictionary<Team, int>();
    public Dictionary<Team, List<Combination>> countedCombinations = new Dictionary<Team, List<Combination>>();
    public Dictionary<Skeleton, PlayerRoundEvaluation> playerEvaluations = new Dictionary<Skeleton, PlayerRoundEvaluation>();
    public List<Team> winners = new List<Team>();
}
