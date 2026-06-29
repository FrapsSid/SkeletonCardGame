using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Шарнир: 6 карт на 5 последовательных рангах, где центральный ранг повторен
    /// Форма: r, r+1, r+2, r+2, r+3, r+4
    /// Схема: 2+4 (пара центрального ранга - источник 1; 4 внешние карты - источник 2)
    /// </summary>
    public class Hinge : TemplateBasedRule
    {
        public override string Name => "Шарнир";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsPair),
                new PartitionTemplate.BlockRequirement(4)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 6) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            // Проверяем паттерн: r, r+1, r+2, r+2, r+3, r+4
            if (ranks[0] + 1 != ranks[1]) return false;
            if (ranks[1] + 1 != ranks[2]) return false;
            if (ranks[2] != ranks[3]) return false;
            if (ranks[3] + 1 != ranks[4]) return false;
            if (ranks[4] + 1 != ranks[5]) return false;

            return true;
        }

        private bool IsPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].Value == cards[1].Value;
        }
    }
}
