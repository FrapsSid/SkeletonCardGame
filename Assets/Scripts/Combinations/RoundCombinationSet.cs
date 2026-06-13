using System.Collections.Generic;

public class RoundCombinationSet
{
    public Combination easyCombination;
    public Combination mediumCombination;
    public Combination hardCombination;
    public Combination antiCombination;

    public RoundCombinationSet(
        Combination easyCombination,
        Combination mediumCombination,
        Combination hardCombination,
        Combination antiCombination)
    {
        this.easyCombination = easyCombination;
        this.mediumCombination = mediumCombination;
        this.hardCombination = hardCombination;
        this.antiCombination = antiCombination;
    }

    public List<(Combination combination, CombinationDifficulty difficulty)> GetAll()
    {
        return new List<(Combination combination, CombinationDifficulty difficulty)>
        {
            (easyCombination, CombinationDifficulty.Easy),
            (mediumCombination, CombinationDifficulty.Medium),
            (hardCombination, CombinationDifficulty.Hard),
            (antiCombination, CombinationDifficulty.Anti)
        };
    }

    public List<(Combination combination, CombinationDifficulty difficulty)> GetScoringCombinations()
    {
        return new List<(Combination combination, CombinationDifficulty difficulty)>
        {
            (easyCombination, CombinationDifficulty.Easy),
            (mediumCombination, CombinationDifficulty.Medium),
            (hardCombination, CombinationDifficulty.Hard)
        };
    }

    public Combination GetCombination(CombinationDifficulty difficulty)
    {
        switch (difficulty)
        {
            case CombinationDifficulty.Easy:
                return easyCombination;
            case CombinationDifficulty.Medium:
                return mediumCombination;
            case CombinationDifficulty.Hard:
                return hardCombination;
            case CombinationDifficulty.Anti:
                return antiCombination;
            default:
                return null;
        }
    }
}
