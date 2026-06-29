using Combinations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Combinations
{
    public class Combination
    {
        private readonly CombinationRule _rule;
        private readonly int _requiredCardCount;

        public string DisplayName => _rule.Name;
        //public string Description => _rule.Description;
        public CombinationRule Rule => _rule;
        public int RequiredCardCount => _requiredCardCount;

        public Combination(CombinationRule combinationRule)
        {
            _rule = combinationRule;
            //_requiredCardCount = combinationRule.RequiredCardCount;
        }

        public bool IsSatisfied(List<CardWithPool> cards)
        {
            if (cards == null || cards.Count < _requiredCardCount) return false;

            return _rule.Check(cards);
        }
    }
}
