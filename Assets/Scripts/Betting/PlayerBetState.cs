using System;

[Serializable]
public class PlayerBetState
{
    public bool folded;
    public DeclaredCombinationTier declaredTarget;

    public PlayerBetState()
    {
    }

    public PlayerBetState(DeclaredCombinationTier declaredTarget)
    {
        this.declaredTarget = declaredTarget;
    }

    public PlayerBetState(DeclaredCombinationTier declaredTarget, bool folded)
    {
        this.declaredTarget = declaredTarget;
        this.folded = folded;
    }
}
