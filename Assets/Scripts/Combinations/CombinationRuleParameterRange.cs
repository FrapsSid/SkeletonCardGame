using System;

[Serializable]
public class CombinationRuleParameterRange
{
    public CombinationRuleType ruleType;
    public int minCardCount;
    public int maxCardCount;
    public int minValue;
    public int maxValue;
    public bool usesCardCount = true;
    public bool usesValue;

    public CombinationRuleParameterRange()
    {
    }

    public CombinationRuleParameterRange(
        CombinationRuleType ruleType,
        int minCardCount,
        int maxCardCount,
        int minValue = 0,
        int maxValue = 0,
        bool usesCardCount = true,
        bool usesValue = false)
    {
        this.ruleType = ruleType;
        this.minCardCount = minCardCount;
        this.maxCardCount = maxCardCount;
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.usesCardCount = usesCardCount;
        this.usesValue = usesValue;
    }

    public CombinationRule CreateRule(System.Random random, int fallbackCardCount)
    {
        int paramN = usesCardCount
            ? random.Next(minCardCount, maxCardCount + 1)
            : fallbackCardCount;

        int paramValue = usesValue
            ? random.Next(minValue, maxValue + 1)
            : 0;

        return new CombinationRule(ruleType, paramN, paramValue);
    }
}
