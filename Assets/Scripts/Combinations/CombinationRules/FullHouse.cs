using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Фулл: 3 карты одного ранга и 2 карты другого ранга
    /// Схема: 3+2 (тройка целиком - источник 1, пара целиком - источник 2)
    /// </summary>
    public class FullHouse : TemplateBasedRule
    {
        public override string Name => "Фулл";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(3, IsThreeOfKind),
                new PartitionTemplate.BlockRequirement(2, IsPair)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.HasRankGroups(cards, new[] { 3, 2 });
        }

        private bool IsThreeOfKind(List<CardData> cards)
        {
            if (cards.Count != 3) return false;
            return cards[0].Value == cards[1].Value && cards[1].Value == cards[2].Value;
        }

        private bool IsPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].Value == cards[1].Value;
        }
    }
}
