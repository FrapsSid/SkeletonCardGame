using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Двойная Ступень: 2 пары соседних рангов
    /// Схема: 2+2 (каждая пара целиком в своем источнике)
    /// </summary>
    public class DoubleStep : TemplateBasedRule
    {
        public override string Name => "Двойная Ступень";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsPair),
                new PartitionTemplate.BlockRequirement(2, IsPair)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsConsecutivePairs(cards, 2);
        }

        private bool IsPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].Value == cards[1].Value;
        }
    }
}
