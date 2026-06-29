using System;
using System.Collections.Generic;

namespace Combinations
{
    public class CombinationGenerator
    {
        private readonly System.Random _random;

        public CombinationGenerator(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public RoundCombinationSet GenerateRoundCombinations()
        {
            Combination easyCombination = new Combination(CombinationRuleRegistry.EasyRules[_random.Next(0, CombinationRuleRegistry.EasyRules.Count)]);
            Combination mediumCombination = new Combination(CombinationRuleRegistry.MediumRules[_random.Next(0, CombinationRuleRegistry.MediumRules.Count)]);
            Combination hardCombination = new Combination(CombinationRuleRegistry.HardRules[_random.Next(0, CombinationRuleRegistry.HardRules.Count)]);
            Combination antiCombination = new Combination(CombinationRuleRegistry.AntiRules[_random.Next(0, CombinationRuleRegistry.AntiRules.Count)]);

            RoundCombinationSet combinationSet = new RoundCombinationSet(easyCombination, mediumCombination, hardCombination, antiCombination);

            return combinationSet;
        }
    }
}
