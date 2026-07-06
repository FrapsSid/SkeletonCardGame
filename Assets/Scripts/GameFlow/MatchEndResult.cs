using System.Collections.Generic;

public sealed class MatchEndResult
{
    public MatchEndResult(Team winningTeam, IReadOnlyList<Team> activeTeams, IReadOnlyList<Team> eliminatedTeams)
    {
        WinningTeam = winningTeam;
        ActiveTeams = activeTeams ?? new List<Team>();
        EliminatedTeams = eliminatedTeams ?? new List<Team>();
    }

    public Team WinningTeam { get; }
    public IReadOnlyList<Team> ActiveTeams { get; }
    public IReadOnlyList<Team> EliminatedTeams { get; }
    public bool HasWinner => WinningTeam != null;
}
