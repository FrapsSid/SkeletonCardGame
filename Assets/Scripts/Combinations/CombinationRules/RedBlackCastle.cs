using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Красно-Черный Замок: 4 карты одного ранга, ровно 2 красные и 2 черные
    /// Схема: 2+2 (2 красные карты этого ранга - источник 1; 2 черные карты этого ранга - источник 2)
    /// </summary>
    public class RedBlackCastle : TemplateBasedRule
    {
        public override string Name => "Красно-Черный Замок";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsRedPair),
                new PartitionTemplate.BlockRequirement(2, IsBlackPair)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 4) return false;

            // Все карты одного ранга
            var firstRank = cards[0].Value;
            if (!cards.All(c => c.Value == firstRank))
                return false;

            // Ровно 2 красные и 2 черные
            var redCount = cards.Count(c => c.IsRed);
            var blackCount = cards.Count(c => c.IsBlack);

            return redCount == 2 && blackCount == 2;
        }

        private bool IsRedPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            
            // Обе карты красные и одного ранга
            return cards[0].IsRed && cards[1].IsRed && cards[0].Value == cards[1].Value;
        }

        private bool IsBlackPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            
            // Обе карты черные и одного ранга
            return cards[0].IsBlack && cards[1].IsBlack && cards[0].Value == cards[1].Value;
        }
    }
}
