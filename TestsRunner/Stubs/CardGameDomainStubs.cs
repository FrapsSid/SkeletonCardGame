#nullable enable

namespace UnityEngine
{
    public static class Debug
    {
        public static void LogWarning(object message)
        {
            System.Console.WriteLine(message);
        }
    }
}

public sealed class SkeletonBody
{
}

public sealed class PlayerInventoryOwner
{
}

public enum StakeAssetType
{
    BodyPart,
    Soul,
    OtherTeamAsset
}

public sealed class StakeAsset
{
    public StakeAsset(Team owningTeam, StakeAssetType assetType, int stakeValue, Skeleton? sourceOwner = null)
    {
        this.owningTeam = owningTeam;
        this.assetType = assetType;
        this.stakeValue = stakeValue;
        this.sourceOwner = sourceOwner;
    }

    public Team owningTeam;
    public Skeleton? sourceOwner;
    public StakeAssetType assetType;
    public int stakeValue;

    public void TransferOwnership(Team newOwner)
    {
        if (owningTeam == newOwner)
            return;

        owningTeam?.UnregisterAsset(this);
        owningTeam = newOwner;
        owningTeam?.RegisterAsset(this);
    }
}

public sealed class RoundScorer
{
    public RoundResult CalculateRoundResults(
        System.Collections.Generic.List<Team> teams,
        System.Collections.Generic.List<CardData> tableCards,
        Combinations.RoundCombinationSet combos,
        System.Collections.Generic.Dictionary<Skeleton, PlayerBetState> playerStates)
    {
        RoundResult result = new RoundResult();

        if (teams.Count > 0)
            result.winners.Add(teams[0]);

        return result;
    }
}
