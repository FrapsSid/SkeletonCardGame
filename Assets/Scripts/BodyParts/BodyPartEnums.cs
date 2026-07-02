#nullable enable

public enum BodyPartType
{
    Head,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    Soul,
    Torso
}

public enum BodyPartState
{
    Attached,
    Detached
}

public static class BodyPartTypeExtensions
{
    public static int BodyPartCost(this BodyPartType type)
    {
        switch (type)
        {
            case BodyPartType.Head:
                return 3;
            case BodyPartType.LeftArm:
                return 2;
            case BodyPartType.RightArm:
                return 2;
            case BodyPartType.LeftLeg:
                return 2;
            case BodyPartType.RightLeg:
                return 2;
            case BodyPartType.Soul:
                return 6;
            case BodyPartType.Torso:
                return 1;
            default:
                return 1;
        }
    }
}

public static class BodyPartExtensions
{
    public static int GetBodyPartCost(BodyPart part)
    {
        return part.Type.BodyPartCost();
    }
}
