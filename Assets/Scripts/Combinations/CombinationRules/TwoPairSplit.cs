using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Две Пары С Расколом: 2 разных ранга, по 2 карты каждого
    /// Схема: каждый источник дает по одной карте каждого ранга (перекрестное разбиение)
    /// </summary>
    public class TwoPairSplit : TemplateBasedRule
    {
        public override string Name => "Two Pair Split";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsNotPair),
                new PartitionTemplate.BlockRequirement(2, IsNotPair)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsTwoPairs(cards);
        }

        private bool IsNotPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].Value != cards[1].Value;
        }
    }
}
