using System.Collections.Generic;

public class AIResponsePackage
{
    public AIActionType Action { get; private set; }
    public int RaiseAmount { get; private set; }
    public List<BodyPart> PutOnStakeParts { get; private set; }
    public DeclaredCombinationTier? ChosenTarget { get; private set; }

    public AIResponsePackage(
        AIActionType action,
        int raiseAmount = 0,
        List<BodyPart> stakeParts = null,
        DeclaredCombinationTier? chosenTarget = null)
    {
        Action = action;
        RaiseAmount = raiseAmount;
        PutOnStakeParts = stakeParts ?? new List<BodyPart>();
        ChosenTarget = chosenTarget;
    }
}
