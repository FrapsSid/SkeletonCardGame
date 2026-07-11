//Skeleton Class Placeholder
public class Skeleton
{
    public Hand Hand { get; private set; }
    public readonly Team team;
    public SkeletonBody Body { get; private set; }
    public PlayerInventoryOwner InventoryOwner { get; private set; }
    public bool HasNetworkClientId { get; private set; }
    public ulong NetworkClientId { get; private set; }

    // ═══ SOUL = life. No soul = ghost/spectator ═══
    public bool HasSoul
    {
        get
        {
            if (team == null) return true;
            foreach (var asset in team.Assets)
            {
                if (asset.assetType == StakeAssetType.Soul
                    && asset.sourceOwner == this
                    && asset.owningTeam == team)
                    return true;
            }
            return false;
        }
    }

    /// <summary>A skeleton that lost its Soul becomes a Ghost (spectator).</summary>
    public bool IsGhost => !HasSoul;

    // ═══ Body part gameplay effects ═══
    // StakeAssetType = { BodyPart, Soul, OtherTeamAsset }
    // Actual body part kind: asset.bodyPart.Item.Type (BodyPartType)
    public bool CanHoldCards =>
        CountOwnedBodyPartsByType(BodyPartType.LeftArm) +
        CountOwnedBodyPartsByType(BodyPartType.RightArm) > 0;

    public bool HasProperVision =>
        CountOwnedBodyPartsByType(BodyPartType.Head) > 0;

    public int MaxInventorySlots
    {
        get
        {
            int arms = CountOwnedBodyPartsByType(BodyPartType.LeftArm)
                     + CountOwnedBodyPartsByType(BodyPartType.RightArm);
            return 2 + (arms * 2);
        }
    }

    public Skeleton(Team team)
    {
        Hand = new Hand();
        this.team = team;
    }

    public void SetBody(SkeletonBody body)
    {
        Body = body;
        if (body != null)
            body.SetOwner(this);
    }

    public void SetInventoryOwner(PlayerInventoryOwner inventoryOwner)
    {
        InventoryOwner = inventoryOwner;
    }

    public void SetNetworkClientId(ulong clientId)
    {
        NetworkClientId = clientId;
        HasNetworkClientId = true;
    }

    public void ClearNetworkClientId()
    {
        NetworkClientId = 0;
        HasNetworkClientId = false;
    }

    private int CountOwnedBodyPartsByType(BodyPartType partType)
    {
        if (team == null) return 0;
        int count = 0;
        foreach (var asset in team.Assets)
        {
            if (asset.owningTeam != team) continue;
            if (asset.sourceOwner != this) continue;
            if (asset.assetType != StakeAssetType.BodyPart) continue;
            if (asset.bodyPart == null) continue;
            if (asset.bodyPart.Item.Type == partType)
                count++;
        }
        return count;
    }
}
