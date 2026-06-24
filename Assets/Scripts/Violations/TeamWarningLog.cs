#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class TeamWarningResult {
    public TeamWarningResult(
        Team warnedTeam,
        int warningCount,
        bool compensationApplied,
        bool isDefeated,
        IReadOnlyDictionary<Team, int> warningSnapshot) {
        WarnedTeam = warnedTeam;
        WarningCount = warningCount;
        CompensationApplied = compensationApplied;
        IsDefeated = isDefeated;
        WarningSnapshot = warningSnapshot;
    }

    public Team WarnedTeam { get; }
    public int WarningCount { get; }
    public bool CompensationApplied { get; }
    public bool IsDefeated { get; }
    public IReadOnlyDictionary<Team, int> WarningSnapshot { get; }
}

public sealed class TeamWarningLog {
    private readonly Dictionary<Team, int> warnings = new Dictionary<Team, int>();
    private readonly HashSet<Team> defeatedTeams = new HashSet<Team>();

    public int WarningsToLose { get; }
    public IReadOnlyCollection<Team> DefeatedTeams => defeatedTeams;

    public TeamWarningLog(int warningsToLose = 2) {
        if (warningsToLose < 1) {
            throw new ArgumentOutOfRangeException(nameof(warningsToLose));
        }

        WarningsToLose = warningsToLose;
    }

    public void RegisterTeam(Team? team) {
        if (team == null) {
            return;
        }

        if (!warnings.ContainsKey(team))
            warnings[team] = 0;
    }

    public void RegisterTeams(IEnumerable<Team> teams) {
        if (teams == null)
            return;

        foreach (Team team in teams)
            RegisterTeam(team);
    }

    public int GetWarnings(Team team) {
        if (team == null)
            return 0;

        return warnings.TryGetValue(team, out int count) ? count : 0;
    }

    public IReadOnlyDictionary<Team, int> GetWarningSnapshot() {
        return new Dictionary<Team, int>(warnings);
    }

    public TeamWarningResult AddWarning(Team team) {
        if (team == null)
            throw new ArgumentNullException(nameof(team));

        RegisterTeam(team);
        warnings[team] = GetWarnings(team) + 1;

        bool compensationApplied = ApplyAllSidesCheatedCompensation();
        int countAfterCompensation = GetWarnings(team);
        RebuildDefeatedTeams();
        bool isDefeated = defeatedTeams.Contains(team);

        return new TeamWarningResult(
            team,
            countAfterCompensation,
            compensationApplied,
            isDefeated,
            GetWarningSnapshot());
    }

    public void Clear() {
        warnings.Clear();
        defeatedTeams.Clear();
    }

    private bool ApplyAllSidesCheatedCompensation() {
        if (warnings.Count < 2)
            return false;

        if (warnings.Values.Any(count => count <= 0))
            return false;

        List<Team> teams = warnings.Keys.ToList();
        foreach (Team team in teams)
            warnings[team] = Math.Max(0, warnings[team] - 1);

        return true;
    }

    private void RebuildDefeatedTeams() {
        defeatedTeams.Clear();
        foreach (var pair in warnings) {
            if (pair.Value >= WarningsToLose)
                defeatedTeams.Add(pair.Key);
        }
    }
}
