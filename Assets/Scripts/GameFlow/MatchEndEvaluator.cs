using System.Collections.Generic;
using System.Linq;

public sealed class MatchEndEvaluator
{
    public MatchEndResult Evaluate(IReadOnlyList<Team> teams)
    {
        if (teams == null || teams.Count == 0)
            return null;

        List<StakeAsset> allAssets = teams
            .Where(team => team != null)
            .SelectMany(team => team.Assets)
            .Where(asset => asset != null)
            .Distinct()
            .ToList();

        List<Team> activeTeams = new List<Team>();
        List<Team> eliminatedTeams = new List<Team>();

        foreach (Team team in teams)
        {
            if (team == null || team.Skeletons.Count == 0)
                continue;

            if (TeamRetainsAnySoul(team, allAssets))
                activeTeams.Add(team);
            else
                eliminatedTeams.Add(team);
        }

        if (activeTeams.Count == 1 && eliminatedTeams.Count > 0)
            return new MatchEndResult(activeTeams[0], activeTeams, eliminatedTeams);

        return null;
    }

    private static bool TeamRetainsAnySoul(Team team, IReadOnlyList<StakeAsset> allAssets)
    {
        bool foundAnyPlayerSoul = false;
        bool foundOwnedSoul = false;

        foreach (Skeleton player in team.Skeletons)
        {
            StakeAsset playerSoul = FindPlayerSoul(player, allAssets);
            if (playerSoul == null)
            {
                continue;
            }

            foundAnyPlayerSoul = true;
            if (playerSoul.owningTeam == team)
            {
                foundOwnedSoul = true;
            }
        }

        if (!foundAnyPlayerSoul)
            return false;

        return foundOwnedSoul;
    }

    private static StakeAsset FindPlayerSoul(Skeleton player, IReadOnlyList<StakeAsset> allAssets)
    {
        if (player == null)
            return null;

        foreach (StakeAsset asset in allAssets)
        {
            if (asset != null
                && asset.assetType == StakeAssetType.Soul
                && asset.sourceOwner == player)
            {
                return asset;
            }
        }

        return null;
    }
}
