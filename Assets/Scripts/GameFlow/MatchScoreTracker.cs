using System;
using System.Collections.Generic;
using System.Linq;

public class MatchScoreTracker
{
    private readonly int _targetScore;
    private readonly int _targetRounds;
    private int _roundsPlayed;

    public Dictionary<Team, int> totalScores = new Dictionary<Team, int>();
    public int RoundsPlayed => _roundsPlayed;
    public event Action<Dictionary<Team, int>> OnScoreChanged;

    public MatchScoreTracker(int targetScore = 10, int targetRounds = 0)
    {
        _targetScore = Math.Max(1, targetScore);
        _targetRounds = Math.Max(0, targetRounds);
    }

    public void AddRoundResult(RoundResult result)
    {
        if (result?.scores == null) return;

        _roundsPlayed++;

        foreach (KeyValuePair<Team, int> pair in result.scores)
        {
            if (pair.Key == null) continue;

            if (!totalScores.ContainsKey(pair.Key))
            {
                totalScores[pair.Key] = 0;
            }

            totalScores[pair.Key] += pair.Value;
        }

        OnScoreChanged?.Invoke(new Dictionary<Team, int>(totalScores));
    }

    public List<Team> GetLeaders()
    {
        if (totalScores.Count == 0) return new List<Team>();

        int maxScore = totalScores.Values.Max();
        return totalScores
            .Where(pair => pair.Value == maxScore)
            .Select(pair => pair.Key)
            .ToList();
    }

    public bool CheckMatchWinCondition()
    {
        bool reachedTargetScore = totalScores.Values.Any(score => score >= _targetScore);
        bool reachedTargetRounds = _targetRounds > 0 && _roundsPlayed >= _targetRounds;

        return reachedTargetScore || reachedTargetRounds;
    }
}
