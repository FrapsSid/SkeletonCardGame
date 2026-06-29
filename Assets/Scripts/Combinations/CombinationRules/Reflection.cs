using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Отражение: две одинаковые трехкарточные линии рангов с шагом 2
    /// Форма: r, r+2, r+4 и еще r, r+2, r+4
    /// Схема: 3+3 (одна линия - источник 1; вторая линия - источник 2)
    /// </summary>
    public class Reflection : TemplateBasedRule
    {
        public override string Name => "Отражение";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(3, IsStep2Line),
                new PartitionTemplate.BlockRequirement(3, IsStep2Line)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 6) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            var uniqueRanks = ranks.Distinct().ToList();
            if (uniqueRanks.Count != 3) return false;

            foreach (var rank in uniqueRanks)
            {
                if (ranks.Count(r => r == rank) != 2) return false;
            }

            if (uniqueRanks[1] - uniqueRanks[0] != 2) return false;
            if (uniqueRanks[2] - uniqueRanks[1] != 2) return false;

            return true;
        }

        private bool IsStep2Line(List<CardData> cards)
        {
            if (cards.Count != 3) return false;
            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();
            return ranks[1] - ranks[0] == 2 && ranks[2] - ranks[1] == 2;
        }
    }
}
