//Skeleton Class Placeholder
public class Skeleton
{
    public Hand Hand { get; private set; }
    public readonly Team team;
    public SkeletonBody Body { get; private set; }
    public PlayerInventoryOwner InventoryOwner { get; private set; }
    public bool HasNetworkClientId { get; private set; }
    public ulong NetworkClientId { get; private set; }

    public Skeleton(Team team)
    {
        Hand = new Hand();
        this.team = team;
    }

    public void SetBody(SkeletonBody body)
    {
        Body = body;
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
}
