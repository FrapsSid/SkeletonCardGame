using System.Collections.Generic;
using Combinations;

public struct GameStateSnapshot
{
    public int CurrentIteration;
    public RoundCombinationSet RoundCombinations;
    public List<CardData> TableCards;

    public AIDeck _AIDeck;

    public SkeletonBody OwnBody;
    public List<CardData> OwnHand;
    public DeclaredCombinationTier? OwnTarget;
    public int OwnCommittedValue;

    public List<CardData> AllyHand;
    public DeclaredCombinationTier? AllyTarget;

    public List<CardData> Enemy1Hand;
    public DeclaredCombinationTier? Enemy1Target;
    public List<CardData> Enemy2Hand;
    public DeclaredCombinationTier? Enemy2Target;

    public int CurrentParticipationPrice;
    public int PotSize;
}