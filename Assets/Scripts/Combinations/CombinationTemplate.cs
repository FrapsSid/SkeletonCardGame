using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class CombinationTemplate
{
    public CombinationDifficulty difficulty;
    public int minCardCount;
    public int maxCardCount;
    public int modifierCount;
    public List<CombinationRuleType> possibleRules = new List<CombinationRuleType>();
    public List<CombinationRuleParameterRange> parameterRanges = new List<CombinationRuleParameterRange>();

    public CombinationTemplate()
    {
    }

    public CombinationTemplate(
        CombinationDifficulty difficulty,
        int minCardCount,
        int maxCardCount,
        int modifierCount,
        IEnumerable<CombinationRuleType> possibleRules,
        IEnumerable<CombinationRuleParameterRange> parameterRanges)
    {
        this.difficulty = difficulty;
        this.minCardCount = minCardCount;
        this.maxCardCount = maxCardCount;
        this.modifierCount = modifierCount;
        this.possibleRules = possibleRules?.ToList() ?? new List<CombinationRuleType>();
        this.parameterRanges = parameterRanges?.ToList() ?? new List<CombinationRuleParameterRange>();
    }

    public CombinationRuleParameterRange GetRange(CombinationRuleType ruleType)
    {
        return parameterRanges.FirstOrDefault(range => range.ruleType == ruleType);
    }

    public static CombinationTemplate CreateDefault(CombinationDifficulty difficulty)
    {
        switch (difficulty)
        {
            case CombinationDifficulty.Easy:
                return CreateDefault(difficulty, 2, 3, 1);
            case CombinationDifficulty.Medium:
            case CombinationDifficulty.Anti:
                return CreateDefault(difficulty, 3, 4, 2);
            case CombinationDifficulty.Hard:
                return CreateDefault(difficulty, 4, 5, 3);
            default:
                return CreateDefault(CombinationDifficulty.Easy, 2, 3, 1);
        }
    }

    private static CombinationTemplate CreateDefault(
        CombinationDifficulty difficulty,
        int minCardCount,
        int maxCardCount,
        int modifierCount)
    {
        var possibleRules = new[]
        {
            CombinationRuleType.SameSuit,
            CombinationRuleType.SameRank,
            CombinationRuleType.Sequence,
            CombinationRuleType.SequenceSameSuit,
            CombinationRuleType.SumGreaterThan,
            CombinationRuleType.SumLessThan,
            CombinationRuleType.AllDifferentSuits,
            CombinationRuleType.AllDifferentRanks,
            CombinationRuleType.ContainsRank
        };

        var ranges = new List<CombinationRuleParameterRange>
        {
            new CombinationRuleParameterRange(CombinationRuleType.SameSuit, minCardCount, maxCardCount),
            new CombinationRuleParameterRange(CombinationRuleType.SameRank, minCardCount, Math.Min(maxCardCount, 4)),
            new CombinationRuleParameterRange(CombinationRuleType.Sequence, minCardCount, maxCardCount),
            new CombinationRuleParameterRange(CombinationRuleType.SequenceSameSuit, minCardCount, maxCardCount),
            new CombinationRuleParameterRange(CombinationRuleType.AllDifferentSuits, minCardCount, Math.Min(maxCardCount, 4)),
            new CombinationRuleParameterRange(CombinationRuleType.AllDifferentRanks, minCardCount, maxCardCount),
            new CombinationRuleParameterRange(CombinationRuleType.SumGreaterThan, 0, 0, minCardCount * 4, maxCardCount * 10, false, true),
            new CombinationRuleParameterRange(CombinationRuleType.SumLessThan, 0, 0, minCardCount * 7, maxCardCount * 12, false, true),
            new CombinationRuleParameterRange(CombinationRuleType.ContainsRank, 0, 0, 2, 14, false, true)
        };

        return new CombinationTemplate(difficulty, minCardCount, maxCardCount, modifierCount, possibleRules, ranges);
    }
}
