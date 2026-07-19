using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Перекрест Мастей: две ранговые пары в двух мастях
    /// Форма: A♠, A♥, B♠, B♥
    /// Схема: A♠ и B♥ - источник 1; A♥ и B♠ - источник 2 (диагональный обмен)
    /// </summary>
    public class SuitCross : TemplateBasedRule
    {
        public override string Name => "Suit Cross";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsDiagonal),
                new PartitionTemplate.BlockRequirement(2, IsDiagonal)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 4) return false;

            var rankCount = cards.GroupBy(c => c.Value).Count();
            if (rankCount != 2) return false;

            var suitCount = cards.GroupBy(c => c.Suit).Count();
            if (suitCount != 2) return false;

            var rankGroups = cards.GroupBy(c => c.Value).ToList();
            if (!rankGroups.All(g => g.Count() == 2)) return false;

            var suitGroups = cards.GroupBy(c => c.Suit).ToList();
            if (!suitGroups.All(g => g.Count() == 2)) return false;

            return true;
        }

        private bool IsDiagonal(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            // Диагональная пара: разные ранги И разные масти
            return cards[0].Value != cards[1].Value && cards[0].Suit != cards[1].Suit;
        }
    }
}
