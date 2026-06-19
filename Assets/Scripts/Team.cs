using System.Collections.Generic;

public class Team {
    public List<Skeleton> Skeletons { get; private set; } = new List<Skeleton>();
    public List<StakeAsset> Assets { get; private set; } = new List<StakeAsset>();

    public bool HasPlayer(Skeleton player) {
        return player != null && Skeletons.Contains(player);
    }

    public void AddSkeleton(Skeleton player) {
        if (player != null && !Skeletons.Contains(player)) {
            Skeletons.Add(player);
        }
    }

    public void RegisterAsset(StakeAsset asset) {
        if (asset == null) {
            return;
        }

        if (!Assets.Contains(asset)) {
            Assets.Add(asset);
        }

        asset.owningTeam = this;
    }

    public void UnregisterAsset(StakeAsset asset) {
        if (asset == null) {
            return;
        }

        Assets.Remove(asset);
    }

    public bool OwnsAsset(StakeAsset asset) {
        return asset != null && asset.owningTeam == this;
    }
}
