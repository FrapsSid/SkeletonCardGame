using System;

using Player = Skeleton;

[Serializable]
public class StakeAsset
{
    public Team owningTeam;
    public Player sourceOwner;
    public StakeAssetType assetType;
    public BodyPart bodyPart;
    public UnityEngine.Object assetReference;
    public int stakeValue;

    public StakeAsset()
    {
    }

    public StakeAsset(Team owningTeam, StakeAssetType assetType, int stakeValue,
        UnityEngine.Object assetReference = null,
        Player sourceOwner = null,
        BodyPart bodyPart = null)
    {
        this.owningTeam = owningTeam;
        this.assetType = assetType;
        this.stakeValue = stakeValue;
        this.assetReference = assetReference;
        this.sourceOwner = sourceOwner;
        this.bodyPart = bodyPart;
    }

    public UnityEngine.Object ConcreteAsset => bodyPart != null ? bodyPart : assetReference;

    public void TransferOwnership(Team newOwner)
    {
        if (owningTeam == newOwner)
        {
            owningTeam?.RegisterAsset(this);
            return;
        }

        owningTeam?.UnregisterAsset(this);
        owningTeam = newOwner;
        owningTeam?.RegisterAsset(this);

        if (bodyPart != null && bodyPart.State == BodyPartState.Attached)
        {
            SkeletonBody body = bodyPart.currentHolder?.GetComponent<SkeletonBody>();
            if (body != null)
                body.RemovePart(bodyPart.Item.Type);
            else
                bodyPart.Detach();
        }
    }
}
