using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Клещи: пара одного ранга зажата двумя соседними рангами
    /// Форма: r-1, r, r, r+1
    /// Схема: 2+2 (пара r-r - источник 1; фланги r-1 и r+1 - источник 2)
    /// </summary>
    public class Pincers : TemplateBasedRule
    {
        public override string Name => "Клещи";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsPair),
                new PartitionTemplate.BlockRequirement(2, IsFlanks)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 4) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            // Проверяем паттерн: r-1, r, r, r+1
            if (ranks[0] + 1 != ranks[1]) return false;
            if (ranks[1] != ranks[2]) return false;
            if (ranks[2] + 1 != ranks[3]) return false;

            return true;
        }

        private bool IsPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].Value == cards[1].Value;
        }

        private bool IsFlanks(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();
            // Разница между флангами r-1 и r+1 равна ровно 2
            return ranks[1] - ranks[0] == 2;
        }
    }
}
