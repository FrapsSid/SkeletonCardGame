using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Две Пары: 2 разных ранга, по 2 карты каждого
    /// Схема: 2+2 (каждая пара целиком в своем источнике)
    /// </summary>
    public class TwoPair : TemplateBasedRule
    {
        public override string Name => "Две Пары";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsPair),
                new PartitionTemplate.BlockRequirement(2, IsPair)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsTwoPairs(cards);
        }

        private bool IsPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].Value == cards[1].Value;
        }
    }
}
