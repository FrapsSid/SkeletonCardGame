using System.Collections.Generic;

public static class SkeletonStakeLinker
{
    public static List<StakeAsset> RegisterBodyAssets(Team team, Skeleton owner, SkeletonBody body)
    {
        var assets = new List<StakeAsset>();
        if (team == null || body == null) return assets;

        foreach (BodyPart part in body.GetAttachedParts())
        {
            StakeAssetType type = part.Type == BodyPartType.Soul
                ? StakeAssetType.Soul
                : StakeAssetType.BodyPart;

            int value = BodyPartExtensions.GetBodyPartCost(part);
            var asset = new StakeAsset(team, type, value, sourceOwner: owner, bodyPart: part);

            team.RegisterAsset(asset);
            assets.Add(asset);
        }

        return assets;
    }
}