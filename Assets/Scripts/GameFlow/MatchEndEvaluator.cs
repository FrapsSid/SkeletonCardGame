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

            if (TeamRetainsAnyRibcage(team, allAssets))
                activeTeams.Add(team);
            else
                eliminatedTeams.Add(team);
        }

        if (activeTeams.Count == 1 && eliminatedTeams.Count > 0)
            return new MatchEndResult(activeTeams[0], activeTeams, eliminatedTeams);

        return null;
    }

    private static bool TeamRetainsAnyRibcage(Team team, IReadOnlyList<StakeAsset> allAssets)
    {
        bool foundKnownRibcage = false;
        bool hasPlayerWithoutRibcageData = false;

        foreach (Skeleton player in team.Skeletons)
        {
            StakeAsset playerRibcage = FindPlayerRibcage(player, allAssets);
            if (playerRibcage == null)
            {
                hasPlayerWithoutRibcageData = true;
                continue;
            }

            foundKnownRibcage = true;
            if (playerRibcage.owningTeam == team)
                return true;
        }

        if (hasPlayerWithoutRibcageData)
            return true;

        if (foundKnownRibcage)
            return false;

        return true;
    }

    private static StakeAsset FindPlayerRibcage(Skeleton player, IReadOnlyList<StakeAsset> allAssets)
    {
        if (player == null)
            return null;

        foreach (StakeAsset asset in allAssets)
        {
            if (asset != null
                && asset.assetType == StakeAssetType.BodyPart
                && asset.bodyPart != null
                && asset.bodyPart.Item != null
                && asset.bodyPart.Item.Type == BodyPartType.Torso
                && asset.sourceOwner == player)
            {
                return asset;
            }
        }

        return null;
    }
}
