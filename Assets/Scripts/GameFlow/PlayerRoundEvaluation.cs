using System.Collections.Generic;
using Combinations;

public class PlayerRoundEvaluation
{
    public Skeleton player;
    public bool folded;
    public DeclaredCombinationTier declaredTarget;
    public bool antiTriggered;
    public bool declaredCombinationSatisfied;
    public List<Combination> personalScoredCombinations = new List<Combination>();
    public bool contributesToSharedPool;

    public PlayerRoundEvaluation(Skeleton player, DeclaredCombinationTier declaredTarget)
    {
        this.player = player;
        this.declaredTarget = declaredTarget;
    }
}
