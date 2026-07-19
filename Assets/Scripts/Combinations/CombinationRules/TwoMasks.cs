using System.Collections.Generic;
using System.Linq;
using System;

namespace Combinations.Rules
{
    /// <summary>
    /// Две Маски: 2 красные карты одного ранга и 2 черные карты соседнего ранга
    /// Схема: 2+2 (красная пара ранга r - источник 1; черная пара ранга r+1 - источник 2)
    /// </summary>
    public class TwoMasks : TemplateBasedRule
    {
        public override string Name => "Two Masks";

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

            // Получаем красные и черные карты
            var redCards = cards.Where(c => c.IsRed).ToList();
            var blackCards = cards.Where(c => c.IsBlack).ToList();

            if (redCards.Count != 2 || blackCards.Count != 2)
                return false;

            // Красные должны быть одного ранга
            if (redCards[0].Value != redCards[1].Value)
                return false;

            // Черные должны быть одного ранга
            if (blackCards[0].Value != blackCards[1].Value)
                return false;

            // Ранги должны быть соседними (r и r+1)
            var redRank = (int)redCards[0].Value;
            var blackRank = (int)blackCards[0].Value;

            return Math.Abs(redRank - blackRank) == 1;
        }

        private bool IsRedPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].IsRed && cards[1].IsRed && cards[0].Value == cards[1].Value;
        }

        private bool IsBlackPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].IsBlack && cards[1].IsBlack && cards[0].Value == cards[1].Value;
        }
    }
}
