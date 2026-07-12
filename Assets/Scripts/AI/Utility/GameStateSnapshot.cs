using System.Collections.Generic;
using Combinations;

public struct GameStateSnapshot
{
    public RoundCombinationSet RoundCombinations;
    public List<CardData> TableCards;

    public AIDeck _AIDeck;

    public SkeletonBody OwnBody;
    public List<CardData> OwnHand;
    public DeclaredCombinationTier? OwnTarget;
    public int OwnCommittedValue;

    public int CurrentParticipationPrice;
}