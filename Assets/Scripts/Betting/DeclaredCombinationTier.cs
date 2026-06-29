using Combinations;

public enum DeclaredCombinationTier
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public static class DeclaredCombinationTierExtensions
{
    public static CombinationDifficulty ToCombinationDifficulty(this DeclaredCombinationTier tier)
    {
        switch (tier)
        {
            case DeclaredCombinationTier.Easy:
                return CombinationDifficulty.Easy;
            case DeclaredCombinationTier.Medium:
                return CombinationDifficulty.Medium;
            case DeclaredCombinationTier.Hard:
                return CombinationDifficulty.Hard;
            default:
                return CombinationDifficulty.Easy;
        }
    }

    public static bool AllowsDifficulty(this DeclaredCombinationTier tier, Combinations.CombinationDifficulty difficulty)
    {
        return difficulty.IsScoringDifficulty() && (int)tier >= (int)difficulty;
    }
}
